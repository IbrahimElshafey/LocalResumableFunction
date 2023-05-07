using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Hangfire;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public class Scanner
{
    internal FunctionDataContext _context;
    private IResumableFunctionsSettings _settings;
    private ResumableFunctionHandler _handler;
    private IServiceProvider _serviceProvider;
    private readonly ILogger<Scanner> _logger;
    private readonly string Code = DateTime.Now.Ticks.ToString();
    public Scanner(IServiceProvider serviceProvider, ILogger<Scanner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    public async Task Start()
    {
        try
        {
            //prevent concurrent scan in same service
            await semaphoreSlim.WaitAsync();
            await StartScanService();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when scan [{Assembly.GetEntryAssembly().GetName().Name}]");
            throw;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    private async Task StartScanService()
    {
        using var scope = _serviceProvider.CreateScope();
        ScopeInit(scope);

        WriteMessage("Start register method waits.");
        await RegisterMethods(GetAssembliesToScan());




        WriteMessage("Register local methods");
        await RegisterMethodWaitsInType(typeof(LocalRegisteredMethods), null);

        await _context.SaveChangesAsync();

        WriteMessage("Close with no errors.");
        await _context.DisposeAsync();
    }

    private void ScopeInit(IServiceScope scope)
    {
        _settings = scope.ServiceProvider.GetService<IResumableFunctionsSettings>();
#if DEBUG
        _settings.ForceRescan = true;
#endif
        _handler = scope.ServiceProvider.GetService<ResumableFunctionHandler>();
        _handler.SetDependencies(scope.ServiceProvider);
        _context = _handler._context;
    }

    private List<string> GetAssembliesToScan()
    {
        var currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
        var currentFolder = AppContext.BaseDirectory;

        WriteMessage($"Get assemblies to scan in directory [{currentFolder}].");
        //var assemblyPaths = Directory.EnumerateFiles(_currentFolder, "*.dll").Where(IsIncludedInScan).ToList();
        var assemblyPaths = new List<string>
            {
                $"{currentFolder}{currentServiceName}.dll"
            };
        if (_settings.DllsToScan != null)
            assemblyPaths.AddRange(
                _settings.DllsToScan.Select(x => $"{currentFolder}{x}.dll"));
        assemblyPaths = assemblyPaths.Distinct().ToList();
        return assemblyPaths;
    }

    private BindingFlags GetBindingFlags() => BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private async Task UpdateScanDate(ServiceData serviceData)
    {
        await _context.Entry(serviceData).ReloadAsync();
        serviceData.AddLog($"Update last scan date for service [{serviceData.AssemblyName}] to [{DateTime.Now}].");
        if (serviceData != null)
            serviceData.Modified = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private async Task<bool> CheckScan(string assemblyPath, string serviceUrl)
    {
        var currentAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var serviceData = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName);
        var entryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
        var parentId =
            entryAssemblyName == currentAssemblyName ?
            -1 :
            await _context.ServicesData
            .Where(x => x.AssemblyName == entryAssemblyName)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        if (serviceData == null)
        {
            serviceData = new ServiceData
            {
                AssemblyName = currentAssemblyName,
                Url = serviceUrl,
                ParentId = parentId
            };
            _context.ServicesData.Add(serviceData);
            serviceData.AddLog($"Assembly [{currentAssemblyName}] will be scaned.");
            await _context.SaveChangesAsync();
            return true;
        }
        if (File.Exists(assemblyPath) is false)
        {
            string message = $"Assembly file ({assemblyPath}) not exist.";
            _logger.LogError(message);
            serviceData.AddError(message);
            return false;
        }
        var assembly = Assembly.LoadFile(assemblyPath);
        var isReferenceResumableFunction =
            assembly.GetReferencedAssemblies().Any(x => new[]
            {
                        "ResumableFunctions.Handler",
                        "ResumableFunctions.AspNetService"
            }.Contains(x.Name));
        if (isReferenceResumableFunction is false)
        {
            serviceData.AddError($"Not reference ResumableFunction DLLs,Scan canceled for [{assemblyPath}].");
            return false;
        }
        var lastBuildDate = File.GetLastWriteTime(assemblyPath);
        serviceData.Url = serviceUrl;
        serviceData.AddLog($"Check last scan date for assembly [{currentAssemblyName}].");
        bool shouldScan = lastBuildDate > serviceData.Modified;
        if (shouldScan is false)
            serviceData.AddLog($"No need to rescan assembly [{currentAssemblyName}].");
        return shouldScan || _settings.ForceRescan;
    }

    internal async Task RegisterResumableFunction(MethodInfo resumableFunctionMinfo, ServiceData serviceData)
    {
        var isEntryPoint = IsEntryPoint(resumableFunctionMinfo);
        var methodType = isEntryPoint ?
            MethodType.ResumableFunctionEntryPoint :
            MethodType.SubResumableFunction;
        serviceData.AddLog($"Register resumable function [{resumableFunctionMinfo.GetFullName()}] of type [{methodType}]");
        var mi = await _context.methodIdentifierRepo.AddResumableFunctionIdentifier(new MethodData(resumableFunctionMinfo) { MethodType = methodType });
        await _context.SaveChangesAsync();
        if (isEntryPoint)
        {
            var backgroundJobClient = _serviceProvider.GetService<IBackgroundJobClient>();
            backgroundJobClient.Enqueue(() => RegisterResumableFunctionFirstWait(mi.Id));
        }
    }

    public async Task RegisterResumableFunctionFirstWait(int id)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                ScopeInit(scope);
                var mi = await _context.methodIdentifierRepo.GetResumableFunction(id);
                var resumableFunctionMethodInfo = mi.MethodInfo;
                try
                {
                    WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
                    await _handler.RegisterFirstWait(resumableFunctionMethodInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error when register first wait for function [{resumableFunctionMethodInfo.GetFullName()}]");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when RegisterResumableFunctionFirstWait");
        }
    }

    private async Task RegisterMethods(List<string> assemblyPaths)
    {
        var resumableFunctionClasses = new List<Type>();
        foreach (var assemblyPath in assemblyPaths)
        {
            try
            {
                //check if file exist
                WriteMessage($"Start scan assembly [{assemblyPath}]");


                if (await CheckScan(assemblyPath, _settings.CurrentServiceUrl) is false) continue;

                var assembly = Assembly.LoadFile(assemblyPath);
                var serviceData = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == assembly.GetName().Name);
                foreach (var type in assembly.GetTypes())
                {
                    await RegisterMethodWaitsInType(type, serviceData);
                    //await RegisterExternalMethods(type);
                    if (type.IsSubclassOf(typeof(ResumableFunction)))
                        resumableFunctionClasses.Add(type);
                }

                WriteMessage($"Save discovered method waits for assembly [{assemblyPath}].");
                await _context.SaveChangesAsync();

                foreach (var resumableFunctionClass in resumableFunctionClasses)
                    await RegisterResumableFunctionsInClass(resumableFunctionClass);

                await UpdateScanDate(serviceData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when register a method in assembly [{assemblyPath}]");
                throw;
            }
        }
    }

    private async Task RegisterMethodWaitsInType(Type type, ServiceData serviceData)
    {
        try
        {
            //Debugger.Launch();
            var methodWaits = type
                .GetMethods(GetBindingFlags())
                .Where(method =>
                        method.GetCustomAttributes().Any(x => x.TypeId == WaitMethodAttribute.AttributeId));
            foreach (var method in methodWaits)
            {
                if (ValidateMethodWait(method, serviceData))
                {
                    var methodData = new MethodData(method) { MethodType = MethodType.MethodWait };
                    await _context.methodIdentifierRepo.AddWaitMethodIdentifier(methodData);
                    serviceData?.AddLog($"Adding method identifier {methodData}");
                }
                else
                    serviceData?.AddLog($"Can't add method identifier `{method.GetFullName()}` since it does not match the criteria.");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when adding a method identifier of type `MethodWait` for type `{type.FullName}`";
            serviceData?.AddError(errorMsg, ex);
            _logger.LogError(errorMsg, ex);
            throw;
        }

    }

    private bool ValidateMethodWait(MethodInfo method, ServiceData serviceData)
    {
        var result = true;
        if (method.IsGenericMethod)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must not be generic.");
            result = false;
        }
        if (method.ReturnType == typeof(void))
        {
            serviceData?.AddError($"`{method.GetFullName()}` must return a value, void is not allowed.");
            result = false;
        }
        if (method.IsAsyncMethod() && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            serviceData?.AddError($"`{method.GetFullName()}` async method must return Task<T> object.");
            result = false;
        }
        if (method.IsStatic)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must be instance function.");
            result = false;
        }
        if (method.GetParameters().Length != 1)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must have only one parameter.");
            result = false;
        }
        return result;
    }

    private async Task RegisterResumableFunctionsInClass(Type type)
    {
        var currentAssemblyName = type.Assembly.GetName().Name;
        var serviceData = await _context
            .ServicesData
            .FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName);
        serviceData.AddLog($"Try to find resumable functions in type [{type.FullName}]");
        var resumableFunctions = type
            .GetMethods(GetBindingFlags())
            .Where(method => method
                .GetCustomAttributes()
                .Any(attribute =>
                    attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId ||
                    attribute.TypeId == ResumableFunctionAttribute.AttributeId
                ));

        foreach (var resumableFunctionInfo in resumableFunctions)
        {
            if (ValidateResumableFunctionSignature(resumableFunctionInfo, serviceData))
                await RegisterResumableFunction(resumableFunctionInfo, serviceData);
            else
                serviceData.AddError($"Can't register resumable function `{resumableFunctionInfo.GetFullName()}`.");
        }
    }


    private bool IsEntryPoint(MethodInfo resumableFunction)
    {
        return resumableFunction.GetCustomAttributes()
            .Any(attribute => attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId);
    }

    private bool ValidateResumableFunctionSignature(MethodInfo resumableFunction, ServiceData serviceData)
    {
        var result = true;
        if (resumableFunction.ReturnType != typeof(IAsyncEnumerable<Wait>) && resumableFunction.GetParameters().Length != 0)
        {
            var errorMsg =
                $"The resumable function [{resumableFunction.GetFullName()}] must match the signature `IAsyncEnumerable<Wait> {resumableFunction.Name}()`.\n" +
                $"Must have no parameter and return type must be `IAsyncEnumerable<Wait>`";
            serviceData.AddError(errorMsg);
            _logger.LogError(errorMsg);
            result = false;
        }
        if (resumableFunction.IsStatic)
        {
            serviceData.AddError($"Resumable function `{resumableFunction.GetFullName()}` must be instance function.");
            result = false;
        }
        return result;
    }


    private void WriteMessage(string message)
    {
        _logger.LogInformation(message);
    }

}


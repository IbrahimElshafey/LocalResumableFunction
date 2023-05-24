using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Hangfire;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;

public class Scanner
{
    private readonly IServiceProvider _serviceProvider;

    private readonly FunctionDataContext _context;
    private readonly IResumableFunctionsSettings _settings;
    private readonly ILogger<Scanner> _logger;
    private readonly IMethodIdentifierRepository _methodIdentifierRepo;
    private readonly IWaitsRepository _waitsRepository;
    private readonly IFirstWaitProcessor _firstWaitProcessor;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly string _currentServiceName;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private int currentServiceId = -1;

    public Scanner(
        IServiceProvider serviceProvider,
        ILogger<Scanner> logger,
        IMethodIdentifierRepository methodIdentifierRepo,
        IFirstWaitProcessor firstWaitProcessor,
        IResumableFunctionsSettings settings,
        FunctionDataContext context,
        IBackgroundJobClient backgroundJobClient,
        IWaitsRepository waitsRepository,
        BackgroundJobExecutor backgroundJobExecutor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _methodIdentifierRepo = methodIdentifierRepo;
        _firstWaitProcessor = firstWaitProcessor;
        _settings = settings;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _waitsRepository = waitsRepository;
        _currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
        _backgroundJobExecutor = backgroundJobExecutor;
    }

    public async Task Start()
    {
        await _backgroundJobExecutor.Execute(
            $"Scanner_StartServiceScanning_{_currentServiceName}",
            async () =>
            {
                WriteMessage("Start register method waits.");
                await RegisterMethods(GetAssembliesToScan());

                WriteMessage("Register local methods");
                await RegisterMethodWaitsInType(typeof(LocalRegisteredMethods), null);

                await _context.SaveChangesAsync();

                WriteMessage("Close with no errors.");
                await _context.DisposeAsync();
            },
            $"Error when scan [{_currentServiceName}]");

    }


    private void ScopeInit(IServiceScope scope)
    {
#if DEBUG
        _settings.ForceRescan = true;
#endif
    }

    private List<string> GetAssembliesToScan()
    {
        var currentFolder = AppContext.BaseDirectory;

        WriteMessage($"Get assemblies to scan in directory [{currentFolder}].");
        //var assemblyPaths = Directory.EnumerateFiles(_currentFolder, "*.dll").Where(IsIncludedInScan).ToList();
        var assemblyPaths = new List<string>
            {
                $"{currentFolder}{_currentServiceName}.dll"
            };
        if (_settings.DllsToScan != null)
            assemblyPaths.AddRange(
                _settings.DllsToScan.Select(x => $"{currentFolder}{x}.dll"));
        assemblyPaths = assemblyPaths.Distinct().ToList();
        return assemblyPaths;
    }

    private BindingFlags GetBindingFlags() =>
        BindingFlags.DeclaredOnly |
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Static |
        BindingFlags.Instance;

    private async Task UpdateScanDate(ServiceData serviceData)
    {
        await _context.Entry(serviceData).ReloadAsync();
        serviceData.AddLog($"Update last scan date for service [{serviceData.AssemblyName}] to [{DateTime.Now}].");
        if (serviceData != null)
            serviceData.Modified = DateTime.Now;
        await _context.SaveChangesAsync();
    }


    internal async Task RegisterResumableFunction(MethodInfo resumableFunctionMinfo, ServiceData serviceData)
    {

        var entryPointCheck = EntryPointCheck(resumableFunctionMinfo);
        var methodType = entryPointCheck.IsEntry ? MethodType.ResumableFunctionEntryPoint : MethodType.SubResumableFunction;
        serviceData.AddLog($"Register resumable function [{resumableFunctionMinfo.GetFullName()}] of type [{methodType}]");

        var resumableFunctionIdentifier = await _methodIdentifierRepo
            .AddResumableFunctionIdentifier(
            new MethodData(resumableFunctionMinfo)
            {
                MethodType = methodType,
                IsActive = entryPointCheck.IsActive
            }, currentServiceId);
        await _context.SaveChangesAsync();


        if (entryPointCheck.IsEntry && entryPointCheck.IsActive)
        {
            _backgroundJobClient.Enqueue(() => _firstWaitProcessor.RegisterFirstWait(resumableFunctionIdentifier.Id));
        }
        else if (entryPointCheck.IsEntry && !entryPointCheck.IsActive)
        {
            _backgroundJobClient.Enqueue(() => _waitsRepository.RemoveFirstWaitIfExist(resumableFunctionIdentifier.Id));
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

                var dateBeforeScan = DateTime.Now;
                if (await CheckScan(assemblyPath, _settings.CurrentServiceUrl) is false) continue;

                var assembly = Assembly.LoadFile(assemblyPath);
                var serviceData =
                    await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == assembly.GetName().Name);

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
                await DeleteOldScanData(dateBeforeScan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when register a method in assembly [{assemblyPath}]");
                throw;
            }
        }
    }

    private async Task DeleteOldScanData(DateTime dateBeforeScan)
    {
        var currentService = await _context
            .ServicesData
            .FirstAsync(x => x.AssemblyName == _currentServiceName);
        await _context
            .Logs
            .Where(x =>
                x.EntityId == currentService.Id &&
                x.EntityType == nameof(ServiceData) &&
                x.Created < dateBeforeScan)
            .ExecuteDeleteAsync();
    }

    private async Task<bool> CheckScan(string assemblyPath, string serviceUrl)
    {
        var currentAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var serviceData = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName);

        if (serviceData == null)
        {
            serviceData = await AddNewServiceData(serviceUrl, currentAssemblyName);
            currentServiceId = serviceData.ParentId == -1 ? serviceData.Id : serviceData.ParentId;
        }

        if (File.Exists(assemblyPath) is false)
        {
            string message = $"Assembly file ({assemblyPath}) not exist.";
            _logger.LogError(message);
            serviceData.AddError(message);
            return false;
        }

        serviceData.ErrorCounter = 0;
        currentServiceId = serviceData.ParentId == -1 ? serviceData.Id : serviceData.ParentId;

        if (serviceData.ParentId == -1)
        {
            await _context
               .ServicesData
               .Where(x => x.ParentId == serviceData.Id)
               .ExecuteDeleteAsync();
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
            serviceData.AddError($"No reference for ResumableFunction DLLs found,The scan canceled for [{assemblyPath}].");
            return false;
        }

        var lastBuildDate = File.GetLastWriteTime(assemblyPath);
        serviceData.Url = serviceUrl;
        serviceData.AddLog($"Check last scan date for assembly [{currentAssemblyName}].");
        bool shouldScan = lastBuildDate > serviceData.Modified;
        if (shouldScan is false)
            serviceData.AddLog($"No need to rescan assembly [{currentAssemblyName}].");
        if (_settings.ForceRescan)
            serviceData.AddLog($"Will be scanned because force rescan is enabled in Debug mode.", LogType.Warning);
        return shouldScan || _settings.ForceRescan;

        async Task<ServiceData> AddNewServiceData(string serviceUrl, string currentAssemblyName)
        {
            int parentId = await GetParentServiceId(currentAssemblyName);
            ServiceData serviceData = new ServiceData
            {
                AssemblyName = currentAssemblyName,
                Url = serviceUrl,
                ParentId = parentId
            };
            _context.ServicesData.Add(serviceData);
            serviceData.AddLog($"Assembly [{currentAssemblyName}] will be scaned.");
            await _context.SaveChangesAsync();
            return serviceData;
        }

        async Task<int> GetParentServiceId(string currentAssemblyName)
        {
            return
                _currentServiceName == currentAssemblyName ?
                -1 :
                await _context.ServicesData
                .Where(x => x.AssemblyName == _currentServiceName)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
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
                    await _methodIdentifierRepo.AddWaitMethodIdentifier(methodData, currentServiceId);
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
            serviceData?.AddError($"`{method.GetFullName()}` must be instance method.");
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


    private (bool IsEntry, bool IsActive) EntryPointCheck(MethodInfo resumableFunction)
    {
        var resumableFunctionAttribute =
            (ResumableFunctionEntryPointAttribute)resumableFunction.GetCustomAttributes()
            .FirstOrDefault(attribute => attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId);
        bool isFunctionActive = (resumableFunctionAttribute != null && resumableFunctionAttribute.IsActive);
        return (resumableFunctionAttribute != null, isFunctionActive);
    }

    private bool ValidateResumableFunctionSignature(MethodInfo resumableFunction, ServiceData serviceData)
    {
        var result = true;
        if (resumableFunction.ReturnType != typeof(IAsyncEnumerable<Wait>) || resumableFunction.GetParameters().Length != 0)
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
            serviceData.AddError($"Resumable function `{resumableFunction.GetFullName()}` must be instance method.");
            result = false;
        }
        return result;
    }


    private void WriteMessage(string message)
    {
        _logger.LogInformation(message);
    }

}


using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.Core;

public class Scanner
{
    private const string ScannerAppName = "##SCANNER: ";
    internal FunctionDataContext _context;
    private ResumableFunctionHandler _handler;
    private IServiceProvider _serviceProvider;
    private readonly ILogger<Scanner> _logger;

    public Scanner(IServiceProvider serviceProvider, ILogger<Scanner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Start()
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var settings = scope.ServiceProvider.GetService<IResumableFunctionSettings>();
            _handler = scope.ServiceProvider.GetService<ResumableFunctionHandler>();
            _handler.SetDependencies(scope.ServiceProvider);
            _context = _handler._context;
            WriteMessage("Start Scan Resumable Functions##");


            var currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
            var currentFolder = AppContext.BaseDirectory;

            if (await ShouldScan(currentServiceName, settings.ServiceUrl) is false) return;

            WriteMessage($"Load assemblies in current directory [{currentFolder}].");
            //var assemblyPaths = Directory.EnumerateFiles(_currentFolder, "*.dll").Where(IsIncludedInScan).ToList();
            var assemblyPaths = new List<string>
            {
                $"{currentFolder}\\{currentServiceName}.dll",
                $"{currentFolder}\\ResumableFunctions.Core.dll"
            };
            if(settings.DllsToScan!=null) 
                assemblyPaths.AddRange(
                    settings.DllsToScan.Select(x=> $"{currentFolder}\\{x}.dll"));
            WriteMessage("Start register method waits.");
            var resumableFunctions = await RegisterMethodWaits(assemblyPaths);

            foreach (var resumableFunctionClass in resumableFunctions)
                await RegisterResumableFunctionsInClass(resumableFunctionClass);

            WriteMessage("Register local methods");
            await RegisterMethodWaits(typeof(LocalRegisteredMethods));
            await UpdateScanData(currentServiceName, settings.ServiceUrl);
            await _context.SaveChangesAsync();

            WriteMessage("Close with no errors.");
            await _context.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when scan [{Assembly.GetEntryAssembly().GetName().Name}]");
        }
    }
    private BindingFlags GetBindingFlags() => BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private async Task UpdateScanData(string currentServiceName, string serviceUrl)
    {
        WriteMessage($"Update last scan date for service [{currentServiceName}].");
        var scanRecored = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentServiceName);
        if (scanRecored == null)
        {
            scanRecored = new ServiceData
            {
                AssemblyName = currentServiceName,
                Url = serviceUrl,
            };
            _context.ServicesData.Add(scanRecored);
        }
        scanRecored.LastScanDate = DateTime.Now;
    }

    private async Task<bool> ShouldScan(string currentServiceName, string serviceUrl)
    {
        WriteMessage($"Check last scan date for service [{currentServiceName}].");
        var scanRecored = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentServiceName);
        if (scanRecored == null)
        {
            scanRecored = new ServiceData
            {
                AssemblyName = currentServiceName,
                Url = serviceUrl
            };
            _context.ServicesData.Add(scanRecored);
            WriteMessage($"No need to rescan service [{currentServiceName}].");
            return true;
        }
        var lastBuildDate = File.GetLastWriteTime($"{AppContext.BaseDirectory}\\{currentServiceName}.dll");
        scanRecored.Url = serviceUrl;
        return lastBuildDate > scanRecored.LastScanDate;
    }

    internal async Task RegisterResumableFunction(MethodInfo resumableFunction, MethodType type)
    {
        WriteMessage($"Register resumable function [{resumableFunction.GetFullName()}] of type [{type}]");
        var repo = new MethodIdentifierRepository(_context);
        var methodData = new MethodData(resumableFunction);
        var methodId = await repo.GetMethodIdentifierFromDb(methodData);

        if (methodId != null)
        {
            WriteMessage($"Resumable function [{resumableFunction.GetFullName()}] already exist in DB.");
            return;
        }

        methodId = methodData.ToMethodIdentifier();
        methodId.Type = type;
        //methodId.MethodIdentifier.CreateMethodHash();
        _context.MethodIdentifiers.Add(methodId);
        WriteMessage($"Save discovered resumable function [{resumableFunction.GetFullName()}].");
        await _context.SaveChangesAsync();
    }

    private async Task<List<Type>> RegisterMethodWaits(List<string> assemblyPaths)
    {
        var resumableFunctionClasses = new List<Type>();
        foreach (var assemblyPath in assemblyPaths)
            try
            {
                WriteMessage($"Start scan assembly [{assemblyPath}]");

                var assembly = Assembly.LoadFile(assemblyPath);
                var isReferenceLocalResumableFunction =
                    assembly.GetReferencedAssemblies().Any(x => new[] { "ResumableFunctions.Core", "ResumableFunctions.AspNetService" }.Contains(x.Name));
                if (isReferenceLocalResumableFunction is false)
                {
                    WriteMessage($"Not reference LocalResumableFunction.dll,Scan canceled for [{assemblyPath}].");
                }
                else
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        await RegisterMethodWaits(type);
                        await RegisterExternalMethods(type);
                        if (type.IsSubclassOf(typeof(ResumableFunctionLocal)))
                            resumableFunctionClasses.Add(type);
                    }

                    WriteMessage($"Save discovered method waits for assembly [{assemblyPath}].");
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,$"Error when scan assembly [{assemblyPath}]");
            }

        return resumableFunctionClasses;
    }

    private async Task RegisterMethodWaits(Type type)
    {
        //Debugger.Launch();
        var methodWaits = type
            .GetMethods(GetBindingFlags())
            .Where(method =>
                    method.GetCustomAttributes().Any(x => x.TypeId == new WaitMethodAttribute().TypeId));
        foreach (var method in methodWaits)
        {
            await AddMethodWait(new MethodData(method));
        }
    }

    private async Task AddMethodWait(MethodData methodData)
    {
        var repo = new MethodIdentifierRepository(_context);
        var methodId = await repo.GetMethodIdentifierFromDb(methodData);
        if (methodId != null)
        {
            WriteMessage($"Method [{methodData.MethodName}] already exist in DB.");
            return;
        }
        methodId = 
            _context.MethodIdentifiers.Local.FirstOrDefault(x =>
                x.MethodSignature == methodData.MethodSignature &&
                x.AssemblyName == methodData.AssemblyName &&
                x.ClassName == methodData.ClassName &&
                x.MethodName == methodData.MethodName);
        if (methodId != null)
        {
            WriteMessage($"Method [{methodData.MethodName}] exist in local db.");
            return;
        }
        methodId = methodData.ToMethodIdentifier();
        methodId.Type = MethodType.MethodWait;
        WriteMessage($"Add method [{methodData.MethodName}] to DB.");
        _context.MethodIdentifiers.Add(methodId);
    }

    private async Task RegisterExternalMethods(Type type)
    {
        var externalMethods = type
           .GetMethods(GetBindingFlags())
           .Where(method =>
                   method.GetCustomAttributes().Any(x => x.TypeId == new ExternalWaitMethodAttribute().TypeId))
           .Select(x => new { MethodInfo = x, Attribute = x.GetCustomAttribute(typeof(ExternalWaitMethodAttribute)) });
        foreach (var methodRecord in externalMethods)
        {
            var externalMethodData = new MethodData(methodRecord.MethodInfo);
            var originalMethodData = new MethodData(methodRecord.MethodInfo, (ExternalWaitMethodAttribute)methodRecord.Attribute);
            await AddMethodWait(originalMethodData);

            var externalMethodRecord = await _context.ExternalMethodsRegistry.FirstOrDefaultAsync(x => x.MethodHash == externalMethodData.MethodHash);
            if (externalMethodRecord != null)
            {
                WriteMessage($"Method [{methodRecord.MethodInfo.GetFullName()}] already exist in DB.");
                continue;
            }
            externalMethodRecord = new ExternalMethodRecord
            {
                MethodData = externalMethodData,
                MethodHash = externalMethodData.MethodHash,
                OriginalMethodHash = originalMethodData.MethodHash,
            };
            WriteMessage($"Add external method [{methodRecord.MethodInfo.GetFullName()}] to DB.");
            _context.ExternalMethodsRegistry.Add(externalMethodRecord);
        }
    }

    private async Task RegisterResumableFunctionsInClass(Type type)
    {
        WriteMessage($"Try to find resumable functions in type [{type.FullName}]");
        var resumableFunctions = type
            .GetMethods(GetBindingFlags())
            .Where(method => method
                .GetCustomAttributes()
                .Any(attribute =>
                    attribute.TypeId == new ResumableFunctionEntryPointAttribute().TypeId ||
                    attribute.TypeId == new SubResumableFunctionAttribute().TypeId
                ));

        foreach (var resumableFunction in resumableFunctions)
        {
            if (MatchResumableFunctionSignature(resumableFunction) is false)
            {
                _logger.LogError(
                    $"The resumable function [{resumableFunction.GetFullName()}] must match the signature `IAsyncEnumerable<Wait> {resumableFunction.Name}()`");
                continue;
            }

            var isEntryPoint = IsEntryPoint(resumableFunction);
            var methodType = isEntryPoint ? MethodType.ResumableFunctionEntryPoint : MethodType.SubResumableFunction;
            await RegisterResumableFunction(resumableFunction, methodType);
            if (isEntryPoint)
                await RegisterResumableFunctionFirstWait(resumableFunction);
        }
    }


    private bool IsEntryPoint(MethodInfo resumableFunction)
    {
        return resumableFunction.GetCustomAttributes()
            .Any(attribute => attribute.TypeId == new ResumableFunctionEntryPointAttribute().TypeId);
    }

    private bool MatchResumableFunctionSignature(MethodInfo resumableFunction)
    {
        if (resumableFunction.ReturnType != typeof(IAsyncEnumerable<Wait>))
            return false;
        return resumableFunction.GetParameters().Length == 0;
    }



    internal async Task RegisterResumableFunctionFirstWait(MethodInfo resumableFunction)
    {
        try
        {
            WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
            await _handler.RegisterFirstWait(resumableFunction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when register first wait for function [{resumableFunction.GetFullName()}]");
        }
    }

    private void WriteMessage(string message)
    {
        _logger.LogInformation($"{ScannerAppName}{message}\n");
        //Console.Write(ScannerAppName);
        //Console.WriteLine(message);
    }
}


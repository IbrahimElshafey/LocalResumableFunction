using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public class Scanner
{
    private const string ScannerAppName = "##SCANNER: ";
    internal FunctionDataContext _context;
    private MethodIdentifierRepository _methodIdentifierRepo;
    private IResumableFunctionSettings _settings;
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
            _settings = scope.ServiceProvider.GetService<IResumableFunctionSettings>();
#if DEBUG
            _settings.ForceRescan = true;
#endif
            _handler = scope.ServiceProvider.GetService<ResumableFunctionHandler>();
            _handler.SetDependencies(scope.ServiceProvider);
            _context = _handler._context;
            _methodIdentifierRepo = new MethodIdentifierRepository(_context);


            WriteMessage("Start register method waits.");
            var resumableFunctions = await RegisterMethodWaits(GetAssembliesToScan());

            foreach (var resumableFunctionClass in resumableFunctions)
                await RegisterResumableFunctionsInClass(resumableFunctionClass);

            WriteMessage("Register local methods");
            await RegisterMethodWaitsInType(typeof(LocalRegisteredMethods));

            await _context.SaveChangesAsync();

            WriteMessage("Close with no errors.");
            await _context.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when scan [{Assembly.GetEntryAssembly().GetName().Name}]");
        }
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

    private async Task UpdateScanData(string currentAssemblyName, string serviceUrl)
    {
        WriteMessage($"Update last scan date for service [{currentAssemblyName}].");
        var scanRecored = await _context
            .ServicesData
            .FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName);
        if (scanRecored != null)
            scanRecored.Modified = DateTime.Now;
    }

    private async Task<bool> ShouldScan(string currentAssemblyName, string serviceUrl)
    {
        WriteMessage($"Check last scan date for assembly [{currentAssemblyName}].");
        var scanRecored = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName);
        var entryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
        var parentId =
            entryAssemblyName == currentAssemblyName ?
            -1 :
            await _context.ServicesData
            .Where(x => x.AssemblyName == entryAssemblyName)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        if (scanRecored == null)
        {
            scanRecored = new ServiceData
            {
                AssemblyName = currentAssemblyName,
                Url = serviceUrl,
                ParentId = parentId
            };
            _context.ServicesData.Add(scanRecored);
            return true;
        }
        var lastBuildDate = File.GetLastWriteTime($"{AppContext.BaseDirectory}\\{currentAssemblyName}.dll");
        scanRecored.Url = serviceUrl;
        bool shouldScan = lastBuildDate > scanRecored.Modified;
        if (shouldScan is false)
            WriteMessage($"No need to rescan assembly [{currentAssemblyName}].");
        return shouldScan || _settings.ForceRescan;
    }

    internal async Task RegisterResumableFunction(MethodInfo resumableFunction, MethodType type)
    {
        WriteMessage($"Register resumable function [{resumableFunction.GetFullName()}] of type [{type}]");

        await _methodIdentifierRepo.AddResumableFunctionIdentifier(new MethodData(resumableFunction) { MethodType = type });
        await _context.SaveChangesAsync();
    }



    private async Task<List<Type>> RegisterMethodWaits(List<string> assemblyPaths)
    {
        var resumableFunctionClasses = new List<Type>();
        foreach (var assemblyPath in assemblyPaths)
        {
            try
            {
                //check if file exist
                WriteMessage($"Start scan assembly [{assemblyPath}]");
                if (File.Exists(assemblyPath) is false)
                {
                    _logger.LogError($"Assembly path ({assemblyPath}) not exist.");
                    continue;
                }
                var currentAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (await ShouldScan(currentAssemblyName, _settings.CurrentServiceUrl) is false) continue;
                var assembly = Assembly.LoadFile(assemblyPath);
                var isReferenceResumableFunction =
                    assembly.GetReferencedAssemblies().Any(x => new[]
                    {
                        "ResumableFunctions.Handler",
                        "ResumableFunctions.AspNetService"
                    }.Contains(x.Name));
                if (isReferenceResumableFunction is false)
                {
                    WriteMessage($"Not reference ResumableFunction dlls,Scan canceled for [{assemblyPath}].");
                }
                else
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        await RegisterMethodWaitsInType(type);
                        //await RegisterExternalMethods(type);
                        if (type.IsSubclassOf(typeof(ResumableFunction)))
                            resumableFunctionClasses.Add(type);
                    }

                    WriteMessage($"Save discovered method waits for assembly [{assemblyPath}].");
                    await _context.SaveChangesAsync();

                    await UpdateScanData(currentAssemblyName, _settings.CurrentServiceUrl);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error when scan assembly [{assemblyPath}]");
            }
        }

        return resumableFunctionClasses;
    }

    private async Task RegisterMethodWaitsInType(Type type)
    {
        //Debugger.Launch();
        var methodWaits = type
            .GetMethods(GetBindingFlags())
            .Where(method =>
                    method.GetCustomAttributes().Any(x => x.TypeId == WaitMethodAttribute.AttributeId));
        foreach (var method in methodWaits)
        {
            await _methodIdentifierRepo.AddWaitMethodIdentifier(new MethodData(method) { MethodType = MethodType.MethodWait });
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
                    attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId ||
                    attribute.TypeId == ResumableFunctionAttribute.AttributeId
                ));

        foreach (var resumableFunction in resumableFunctions)
        {
            if (CheckResumableFunctionSignature(resumableFunction) is false)
            {
                _logger.LogError(
                    $"The resumable function [{resumableFunction.GetFullName()}] must match the signature `IAsyncEnumerable<Wait> {resumableFunction.Name}()`");
                continue;
            }

            var isEntryPoint = IsEntryPoint(resumableFunction);
            var methodType = isEntryPoint ?
                MethodType.ResumableFunctionEntryPoint :
                MethodType.SubResumableFunction;
            await RegisterResumableFunction(resumableFunction, methodType);
            if (isEntryPoint)
                await RegisterResumableFunctionFirstWait(resumableFunction);
        }
    }


    private bool IsEntryPoint(MethodInfo resumableFunction)
    {
        return resumableFunction.GetCustomAttributes()
            .Any(attribute => attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId);
    }

    private bool CheckResumableFunctionSignature(MethodInfo resumableFunction)
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


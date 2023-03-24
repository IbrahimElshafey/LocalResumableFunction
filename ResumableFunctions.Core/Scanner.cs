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

    public async Task Start(string serviceUrl)
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            _handler = scope.ServiceProvider.GetService<ResumableFunctionHandler>();
            _handler.SetDependencies(scope.ServiceProvider);
            _context = _handler._context;
            WriteMessage("Start Scan Resumable Functions##");


            var currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
            var currentFolder = AppContext.BaseDirectory;

            if (await ShouldScan(currentServiceName, serviceUrl) is false) return;

            WriteMessage("Load service assemblies in current directory.");
            //var assemblyPaths = Directory.EnumerateFiles(_currentFolder, "*.dll").Where(IsIncludedInScan).ToList();
            var assemblyPaths = new[]
            {
                $"{currentFolder}\\{currentServiceName}.dll",
                $"{currentFolder}\\ResumableFunctions.Core.dll"
            }.ToList();
            WriteMessage("Start register method waits.");
            var resumableFunctions = await RegisterMethodWaits(assemblyPaths);

            foreach (var resumableFunctionClass in resumableFunctions)
                await RegisterResumableFunctionsInClass(resumableFunctionClass);

            WriteMessage("Register local methods");
            await RegisterMethodWaitsIfExist(typeof(LocalRegisteredMethods));
            await UpdateScanData(currentServiceName, serviceUrl);
            await _context.SaveChangesAsync();

            WriteMessage("Close with no errors.");
            await _context.DisposeAsync();
        }
    }

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
        WriteMessage($"Register resumable function [{resumableFunction.Name}] of type [{type}]");
        var repo = new MethodIdentifierRepository(_context);
        var methodData = new MethodData(resumableFunction);
        var methodId = await repo.GetMethodIdentifierFromDb(methodData);

        if (methodId != null)
        {
            WriteMessage($"Resumable function [{resumableFunction.Name}] already exist in DB.");
            return;
        }

        methodId = methodData.ToMethodIdentifier();
        methodId.Type = type;
        //methodId.MethodIdentifier.CreateMethodHash();
        _context.MethodIdentifiers.Add(methodId);
        WriteMessage($"Save discovered resumable function [{resumableFunction.Name}].");
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
                        await RegisterMethodWaitsIfExist(type);
                        if (type.IsSubclassOf(typeof(ResumableFunctionLocal)))
                            resumableFunctionClasses.Add(type);
                    }

                    WriteMessage($"Save discovered method waits for assembly [{assemblyPath}].");
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                WriteMessage($"Error when scan assembly [{assemblyPath}]");
                WriteMessage(e.Message);
            }

        return resumableFunctionClasses;
    }


    private async Task RegisterResumableFunctionsInClass(Type type)
    {
        WriteMessage($"Try to find resumable functions in type [{type.FullName}]");
        var resumableFunctions = type
            .GetMethods(
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
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
                WriteMessage(
                    $"Error the resumable function [{resumableFunction.Name}] must match the signature `IAsyncEnumerable<Wait> {resumableFunction.Name}()`");
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

    private async Task RegisterMethodWaitsIfExist(Type type)
    {
        //Debugger.Launch();
        var methodWaits = type
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method =>
                    method.GetCustomAttributes().Any(x => x.TypeId == new WaitMethodAttribute().TypeId));
        foreach (var method in methodWaits)
        {
            var repo = new MethodIdentifierRepository(_context);
            var methodData = new MethodData(method);
            var methodId = await repo.GetMethodIdentifierFromDb(methodData);
            if (methodId != null)
            {
                WriteMessage($"Method [{method.Name}] already exist in DB.");
                continue;
            }

            methodId = methodData.ToMethodIdentifier();
            methodId.Type = MethodType.MethodWait;
            WriteMessage($"Add method [{method.Name}] to DB.");
            _context.MethodIdentifiers.Add(methodId);
        }
    }

    internal async Task RegisterResumableFunctionFirstWait(MethodInfo resumableFunction)
    {
        WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
        await _handler.RegisterFirstWait(resumableFunction);
    }

    private void WriteMessage(string message)
    {
        _logger.LogInformation($"{ScannerAppName}{message}\n");
        //Console.Write(ScannerAppName);
        //Console.WriteLine(message);
    }
}


﻿using System.Diagnostics;
using System.Reflection;
using LocalResumableFunction;
using LocalResumableFunction.Data;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctionScanner;

internal partial class Scanner
{
    private const string ScannerAppName = "##SCANNER: ";
    private EngineDataContext _context;
    private string _currentFolder;

    public async Task Start()
    {
        //Debugger.Launch();
        WriteMessage("Start Scan Resumable Functions##");
        WriteMessage("Initiate DB context.");
        _context = new EngineDataContext();
        _currentFolder = AppContext.BaseDirectory;
        WriteMessage("Load assemblies in current directory.");
        var assemblies = Directory.EnumerateFiles(_currentFolder, "*.dll");
        foreach (var assemblyPath in assemblies)
            try
            {
                if (ExcludeFromScan(assemblyPath)) continue;
                WriteMessage($"Start scan assembly [{assemblyPath}]");
                await ScanAssembly(assemblyPath);
            }
            catch (Exception e)
            {
                WriteMessage($"Error when scan assembly [{assemblyPath}]");
                WriteMessage(e.Message);
                throw;
            }

        Console.ReadLine();
    }

    private async Task ScanAssembly(string assemblyPath)
    {
        var assembly = Assembly.LoadFile(assemblyPath);
        var isReferenceLocalResumableFunction =
            assembly.GetReferencedAssemblies().Any(x => x.Name == "LocalResumableFunction");
        if (isReferenceLocalResumableFunction is false)
        {
            WriteMessage($"Not reference LocalResumableFunction.dll,Scan canceled for [{assemblyPath}].");
        }
        else
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ResumableFunctionLocal)))
                    await RegisterResumableFunctions(type);
                await RegisterMethodWaitsIfExist(type);
            }
            WriteMessage($"Save scan result for assembly [{assemblyPath}].");
            await _context.SaveChangesAsync();
        }
    }

    private async Task RegisterResumableFunctions(Type type)
    {
        WriteMessage($"Try to find resumable functions in type [{type.FullName}]");
        var resumableFunctions = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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

    private async Task RegisterResumableFunction(MethodInfo resumableFunction, MethodType type)
    {
        WriteMessage($"Register resumable function [{resumableFunction.Name}]");
        var repo = new MethodIdentifierRepository(_context);
        var methodId = await repo.GetMethodIdentifier(resumableFunction);
        if (methodId.ExistInDb)
        {
            WriteMessage($"Resumable function [{resumableFunction.Name}] already exist in DB.");
            return;
        }

        _context.MethodIdentifiers.Add(methodId.MethodIdentifier);
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
        var methodWaits = type.GetMethods().Where(method =>
            method.GetCustomAttributes().Any(x => x.TypeId == new WaitMethodAttribute().TypeId));
        foreach (var method in methodWaits)
        {
            var repo = new MethodIdentifierRepository(_context);
            var methodId = await repo.GetMethodIdentifier(method);
            if (methodId.ExistInDb)
            {
                WriteMessage($"Method [{method.Name}] already exist in DB.");
                return;
            }
            _context.MethodIdentifiers.Add(methodId.MethodIdentifier);
        }
    }

    private bool ExcludeFromScan(string assemblyPath)
    {
        var fileName = Path.GetFileName(assemblyPath);
        var paths = new[]
        {
            "Aq.ExpressionJsonSerializer.dll",
            "LocalResumableFunction.dll",
            "MethodBoundaryAspect.dll",
            "Microsoft.Data.Sqlite.dll",
            "Microsoft.EntityFrameworkCore.Abstractions.dll",
            "Microsoft.EntityFrameworkCore.dll",
            "Microsoft.EntityFrameworkCore.Relational.dll",
            "Microsoft.EntityFrameworkCore.Sqlite.dll",
            "Microsoft.Extensions.Caching.Abstractions.dll",
            "Microsoft.Extensions.Caching.Memory.dll",
            "Microsoft.Extensions.Configuration.Abstractions.dll",
            "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
            "Microsoft.Extensions.DependencyInjection.dll",
            "Microsoft.Extensions.DependencyModel.dll",
            "Microsoft.Extensions.Logging.Abstractions.dll",
            "Microsoft.Extensions.Logging.dll",
            "Microsoft.Extensions.Options.dll",
            "Microsoft.Extensions.Primitives.dll",
            "Newtonsoft.Json.dll",
            "ResumableFunctionScanner.dll",
            "SQLitePCLRaw.batteries_v2.dll",
            "SQLitePCLRaw.core.dll",
            "SQLitePCLRaw.provider.e_sqlite3.dll"
        };
        return paths.Contains(fileName);
    }

    private void WriteMessage(string message)
    {
        Console.Write(ScannerAppName);
        Console.WriteLine(message);
    }
}
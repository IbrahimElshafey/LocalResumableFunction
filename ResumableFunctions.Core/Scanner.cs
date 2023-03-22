using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.Core;

public class Scanner
{
    private const string ScannerAppName = "##SCANNER: ";
    internal FunctionDataContext _context;
    private string _currentFolder;
    private ResumableFunctionHandler _handler;

    public Scanner(ResumableFunctionHandler handler, FunctionDataContext context)
    {
        _handler = handler;
        _context = context;
    }

    public async Task Start()
    {
#if DEBUG
        WriteMessage("DELETE [LocalResumableFunctionsData.db] DATABASE IF EXIST");
        File.Delete($"{AppContext.BaseDirectory}LocalResumableFunctionsData.db");
#endif
        WriteMessage("Start Scan Resumable Functions##");
        WriteMessage("Initiate DB context.");
        _currentFolder = AppContext.BaseDirectory;
        WriteMessage("Load assemblies in current directory.");
        var assemblyPaths = Directory.EnumerateFiles(_currentFolder, "*.dll").Where(IsIncludedInScan).ToList();
        WriteMessage("Start register method waits.");
        var resumableFunctions = await RegisterMethodWaits(assemblyPaths);

        foreach (var resumableFunctionClass in resumableFunctions)
            await RegisterResumableFunctionsInClass(resumableFunctionClass);

        WriteMessage("Register local methods");
        await RegisterMethodWaitsIfExist(typeof(LocalRegisteredMethods));
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
        WriteMessage("Close with no errors.");
        Console.ReadLine();
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
                    assembly.GetReferencedAssemblies().Any(x => x.Name == "LocalResumableFunction");
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

    private bool IsIncludedInScan(string assemblyPath)
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
        return paths.Contains(fileName) is false;
    }

    internal async Task RegisterResumableFunctionFirstWait(MethodInfo resumableFunction)
    {
        WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
        await _handler.RegisterFirstWait(resumableFunction);
    }

    private void WriteMessage(string message)
    {
        Console.Write(ScannerAppName);
        Console.WriteLine(message);
    }
}
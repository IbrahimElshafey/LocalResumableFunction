using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;

internal class Scanner
{
    private readonly FunctionDataContext _context;
    private readonly IResumableFunctionsSettings _settings;
    private readonly ILogger<Scanner> _logger;
    private readonly IMethodIdsRepo _methodIdentifierRepo;
    private readonly IWaitsRepo _waitsRepository;
    private readonly IFirstWaitProcessor _firstWaitProcessor;
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly string _currentServiceName;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IServiceRepo _serviceRepo;

    public Scanner(
        ILogger<Scanner> logger,
        IMethodIdsRepo methodIdentifierRepo,
        IFirstWaitProcessor firstWaitProcessor,
        IResumableFunctionsSettings settings,
        FunctionDataContext context,
        IBackgroundProcess backgroundJobClient,
        IWaitsRepo waitsRepository,
        BackgroundJobExecutor backgroundJobExecutor,
        IServiceRepo serviceRepo)
    {
        _logger = logger;
        _methodIdentifierRepo = methodIdentifierRepo;
        _firstWaitProcessor = firstWaitProcessor;
        _settings = settings;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _waitsRepository = waitsRepository;
        _currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
        _backgroundJobExecutor = backgroundJobExecutor;
        _serviceRepo = serviceRepo;
    }

    public async Task Start()
    {
        await _backgroundJobExecutor.Execute(
            $"Scanner_ScanningService_{_currentServiceName}",
            async () =>
            {
                await RegisterMethods(GetAssembliesToScan());

                await RegisterMethods(typeof(LocalRegisteredMethods), null);

                await _context.SaveChangesAsync();
            },
            $"Error when scan [{_currentServiceName}]", true);

    }

    private List<string> GetAssembliesToScan()
    {
        var currentFolder = AppContext.BaseDirectory;
        _logger.LogInformation($"Get assemblies to scan in directory [{currentFolder}].");
        var assemblyPaths = new List<string>
            {
                $"{currentFolder}{_currentServiceName}.dll"
            };
        if (_settings.DllsToScan != null)
            assemblyPaths.AddRange(_settings.DllsToScan.Select(x => $"{currentFolder}{x}.dll"));

        assemblyPaths = assemblyPaths.Distinct().ToList();
        return assemblyPaths;
    }

    private BindingFlags GetBindingFlags() =>
        BindingFlags.DeclaredOnly |
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Static |
        BindingFlags.Instance;

    internal async Task RegisterResumableFunction(MethodInfo resumableFunctionMInfo, ServiceData serviceData)
    {
        var entryPointCheck = EntryPointCheck(resumableFunctionMInfo);
        var methodType = entryPointCheck.IsEntry ? MethodType.ResumableFunctionEntryPoint : MethodType.SubResumableFunction;
        serviceData.AddLog($"Register resumable function [{resumableFunctionMInfo.GetFullName()}] of type [{methodType}]");

        var resumableFunctionIdentifier = await _methodIdentifierRepo
            .AddResumableFunctionIdentifier(
            new MethodData(resumableFunctionMInfo)
            {
                MethodType = methodType,
                IsActive = entryPointCheck.IsActive
            });
        await _context.SaveChangesAsync();


        switch (entryPointCheck)
        {
            case { IsEntry: true, IsActive: true }:
                _backgroundJobClient.Enqueue(
                    () => _firstWaitProcessor.RegisterFirstWait(resumableFunctionIdentifier.Id));
                break;
            case { IsEntry: true, IsActive: false }:
                _backgroundJobClient.Enqueue(
                    () => _waitsRepository.RemoveFirstWaitIfExist(resumableFunctionIdentifier.Id));
                break;
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
                _logger.LogInformation($"Start scan assembly [{assemblyPath}]");

                var dateBeforeScan = DateTime.Now;
                if (await _serviceRepo.ShouldScanAssembly(assemblyPath) is false) continue;


                var assembly = Assembly.LoadFile(assemblyPath);
                var serviceData = await _serviceRepo.GetServiceData(assembly.GetName().Name);

                foreach (var type in assembly.GetTypes())
                {
                    await RegisterMethods(type, serviceData);
                    //await RegisterExternalMethods(type);
                    if (type.IsSubclassOf(typeof(ResumableFunction)))
                        resumableFunctionClasses.Add(type);
                }

                _logger.LogInformation($"Save discovered method waits for assembly [{assemblyPath}].");
                await _context.SaveChangesAsync();

                foreach (var resumableFunctionClass in resumableFunctionClasses)
                    await RegisterResumableFunctionsInClass(resumableFunctionClass);

                await _serviceRepo.UpdateDllScanDate(serviceData);
                await _serviceRepo.DeleteOldScanData(dateBeforeScan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when register a method in assembly [{assemblyPath}]");
                throw;
            }
        }
    }

    internal async Task RegisterMethods(Type type, ServiceData serviceData)
    {
        try
        {
            //Debugger.Launch();
            var methodWaits = type
                .GetMethods(GetBindingFlags())
                .Where(method =>
                        method.GetCustomAttributes().Any(x => x.TypeId == PushCallAttribute.AttributeId));
            foreach (var method in methodWaits)
            {
                if (ValidateMethod(method, serviceData))
                {
                    var methodData = new MethodData(method) { MethodType = MethodType.MethodWait };
                    await _methodIdentifierRepo.AddWaitMethodIdentifier(methodData);
                    serviceData?.AddLog($"Adding method identifier {methodData}");
                }
                else
                    serviceData?.AddLog(
                        $"Can't add method identifier `{method.GetFullName()}` since it does not match the criteria.");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when adding a method identifier of type `MethodWait` in type `{type.FullName}`";
            serviceData?.AddError(errorMsg, ex);
            _logger.LogError(ex, errorMsg);
            throw;
        }

    }

    private bool ValidateMethod(MethodInfo method, ServiceData serviceData)
    {
        var result = true;
        if (method.IsGenericMethod)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must not be generic.", null, Constants.MethodMustNotBeGeneric);
            result = false;
        }
        if (method.ReturnType == typeof(void))
        {
            serviceData?.AddError($"`{method.GetFullName()}` must return a value, void is not allowed.", null, Constants.MethodMustReturnValue);
            result = false;
        }
        if (method.IsAsyncMethod() && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            serviceData?.AddError($"`{method.GetFullName()}` async method must return Task<T> object.", null, Constants.AsyncMethodMustBeTask);
            result = false;
        }
        if (method.IsStatic)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must be instance method.", null, Constants.MethodMustBeInstance);
            result = false;
        }
        if (method.GetParameters().Length != 1)
        {
            serviceData?.AddError($"`{method.GetFullName()}` must have only one parameter.", null, Constants.MethodMustHaveOneInput);
            result = false;
        }
        return result;
    }

    internal async Task RegisterResumableFunctionsInClass(Type type)
    {
        var serviceData = await _serviceRepo.GetServiceData(type.Assembly.GetName().Name);
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
                serviceData.AddError($"Can't register resumable function `{resumableFunctionInfo.GetFullName()}`.", null, Constants.CantRegisterFunction);
        }
    }


    private (bool IsEntry, bool IsActive) EntryPointCheck(MethodInfo resumableFunction)
    {
        var resumableFunctionAttribute =
            (ResumableFunctionEntryPointAttribute)resumableFunction.GetCustomAttributes()
            .FirstOrDefault(attribute => attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId);
        var isFunctionActive = resumableFunctionAttribute is { IsActive: true };
        return (resumableFunctionAttribute != null, isFunctionActive);
    }

    private bool ValidateResumableFunctionSignature(MethodInfo resumableFunction, ServiceData serviceData)
    {
        var result = true;
        if (!resumableFunction.IsAsyncMethod())
        {
            var errorMsg =
                $"The resumable function [{resumableFunction.GetFullName()}] must be async.";
            serviceData.AddError(errorMsg, null, Constants.FunctionMustBeAsync);
            _logger.LogError(errorMsg);
            result = false;
        }
        if (resumableFunction.ReturnType != typeof(IAsyncEnumerable<Wait>) || resumableFunction.GetParameters().Length != 0)
        {
            var errorMsg =
                $"The resumable function [{resumableFunction.GetFullName()}] must match the signature `IAsyncEnumerable<Wait> {resumableFunction.Name}()`.\n" +
                $"Must have no parameter and return type must be `IAsyncEnumerable<Wait>`";
            serviceData.AddError(errorMsg, null, Constants.FunctionNotMatchSignature);
            _logger.LogError(errorMsg);
            result = false;
        }

        if (!resumableFunction.IsStatic) return result;
        serviceData.AddError($"Resumable function `{resumableFunction.GetFullName()}` must be instance method.", null, Constants.MethodMustBeInstance);
        return false;
    }


}


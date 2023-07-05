using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ResumableFunctions.Handler.Core;

internal class FirstWaitProcessor : IFirstWaitProcessor
{
    private readonly ILogger<FirstWaitProcessor> _logger;
    private readonly IUnitOfWork _context;
    private readonly IMethodIdsRepo _methodIdentifierRepo;
    private readonly IWaitsRepo _waitsRepository;
    private readonly IWaitTemplatesRepo _templatesRepo;
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly IServiceRepo _serviceRepo;

    public FirstWaitProcessor(
        ILogger<FirstWaitProcessor> logger,
        IUnitOfWork context,
        IServiceProvider serviceProvider,
        IMethodIdsRepo methodIdentifierRepo,
        IWaitsRepo waitsRepository,
        BackgroundJobExecutor backgroundJobExecutor,
        IBackgroundProcess backgroundJobClient,
        IServiceRepo serviceRepo,
        IWaitTemplatesRepo templatesRepo)
    {
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
        _methodIdentifierRepo = methodIdentifierRepo;
        _waitsRepository = waitsRepository;
        _backgroundJobExecutor = backgroundJobExecutor;
        _backgroundJobClient = backgroundJobClient;
        _serviceRepo = serviceRepo;
        _templatesRepo = templatesRepo;
    }

    public async Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait)
    {
        var rootId = int.Parse(firstMatchedMethodWait.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)[0]);
        var resumableFunction =
            rootId != firstMatchedMethodWait.Id ?
            await _waitsRepository.GetRequestedByMethodInfo(rootId) :
            firstMatchedMethodWait.RequestedByFunction.MethodInfo;

        try
        {
            var firstWaitClone = await GetFirstWait(resumableFunction, false);
            firstWaitClone.Status = WaitStatus.Temp;
            firstWaitClone.ActionOnWaitsTree(waitClone =>
            {
                waitClone.IsFirst = false;
                waitClone.WasFirst = true;
                waitClone.FunctionState.StateObject = firstMatchedMethodWait?.FunctionState?.StateObject;
                if (waitClone is TimeWait timeWait)
                {
                    timeWait.TimeWaitMethod.ExtraData.JobId = _backgroundJobClient.Schedule(
                        () => new LocalRegisteredMethods().TimeWait(
                        new TimeWaitInput
                        {
                            TimeMatchId = firstMatchedMethodWait.MandatoryPart,
                            Description = $"`{timeWait.Name}` in function `{firstMatchedMethodWait.RequestedByFunction.RF_MethodUrn}:{firstMatchedMethodWait.FunctionState.Id}`"
                        }), timeWait.TimeToWait);
                    timeWait.TimeWaitMethod.MandatoryPart = firstMatchedMethodWait.MandatoryPart;
                    timeWait.IgnoreJobCreation = true;
                }

            });

            firstWaitClone.FunctionState.Logs.AddRange(firstWaitClone.FunctionState.Logs);
            firstWaitClone.FunctionState.Status =
                firstWaitClone.FunctionState.HasErrors() ?
                FunctionStatus.InError :
                FunctionStatus.InProgress;
            await _waitsRepository.SaveWait(firstWaitClone);//first wait clone

            var currentMw = firstWaitClone.GetChildMethodWait(firstMatchedMethodWait.Name);
            currentMw.Status = WaitStatus.Waiting;
            currentMw.Input = firstMatchedMethodWait.Input;
            currentMw.Output = firstMatchedMethodWait.Output;
            var waitTemplate = await _templatesRepo.GetWaitTemplateWithBasicMatch(firstMatchedMethodWait.TemplateId);
            currentMw.TemplateId = waitTemplate.Id;
            currentMw.Template = waitTemplate;
            currentMw.IsFirst = false;
            currentMw.LoadExpressions();
            await _context.SaveChangesAsync();
            firstWaitClone.Status = WaitStatus.Waiting;
            return currentMw;
        }
        catch (Exception ex)
        {
            var error = $"Error when try to clone first wait for function [{resumableFunction.GetFullName()}]";
            await _serviceRepo.AddErrorLog(ex, error, ErrorCodes.FirstWait);
            throw new Exception(error, ex);
        }
    }

    [DisplayName("Register First Wait for Function `{0}`")]
    public async Task RegisterFirstWait(int functionId)
    {
        MethodInfo resumableFunction = null;
        var functionName = "";
        await _backgroundJobExecutor.Execute(
            $"FirstWaitProcessor_RegisterFirstWait_{functionId}",
            (Func<Task>)(async () =>
            {
                try
                {
                    var resumableFunctionId = await _methodIdentifierRepo.GetResumableFunction(functionId);
                    resumableFunction = resumableFunctionId.MethodInfo;
                    functionName = resumableFunction.Name;
                    _logger.LogInformation("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
                    var firstWait = await GetFirstWait(resumableFunction, true);

                    if (firstWait != null)
                    {
                        await _serviceRepo.AddLog(
                            $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.");

                        await _waitsRepository.SaveWait(firstWait);
                        _logger.LogInformation(
                            $"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (resumableFunction != null)
                        await _serviceRepo.AddErrorLog(ex, ErrorMsg(), ErrorCodes.FirstWait);

                    await _waitsRepository.RemoveFirstWaitIfExist(functionId);
                    throw;
                }

            }),
            ErrorMsg(), true);
        string ErrorMsg() => $"Error when try to register first wait for function [{functionName}:{functionId}]";
    }


    public async Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
    {
        var classInstance = (ResumableFunction)Activator.CreateInstance(resumableFunction.DeclaringType);

        if (classInstance == null)
        {

            var errorMsg = $"Can't initiate a new instance of [{resumableFunction.DeclaringType.FullName}]";
            await _serviceRepo.AddErrorLog(null, errorMsg, ErrorCodes.FirstWait);

            throw new NullReferenceException(errorMsg);
        }

        classInstance.InitializeDependencies(_serviceProvider);
        classInstance.CurrentResumableFunction = resumableFunction;
        var functionRunner = new FunctionRunner(classInstance, resumableFunction);
        if (functionRunner.ResumableFunctionExistInCode is false)
        {
            var message = $"Resumable function ({resumableFunction.GetFullName()}) not exist in code.";
            _logger.LogWarning(message);
            await _serviceRepo.AddErrorLog(null, message, ErrorCodes.FirstWait);

            throw new NullReferenceException(message);
        }

        await functionRunner.MoveNextAsync();
        var firstWait = functionRunner.Current;

        if (firstWait == null)
        {
            await _serviceRepo.AddErrorLog(null, $"Can't get first wait in function `{resumableFunction.GetFullName()}`.", ErrorCodes.FirstWait);
            return null;
        }

        var methodId = await _methodIdentifierRepo.GetResumableFunction(new MethodData(resumableFunction));
        if (removeIfExist)
        {
            _logger.LogInformation("First wait already exist it will be deleted and recreated since it may be changed.");
            await _waitsRepository.RemoveFirstWaitIfExist(methodId.Id);
        }
        var functionState = new ResumableFunctionState
        {
            ResumableFunctionIdentifier = methodId,
            StateObject = classInstance,
        };
        firstWait.ActionOnWaitsTree(x =>
        {
            x.RequestedByFunction = methodId;
            x.RequestedByFunctionId = methodId.Id;
            x.IsFirst = true;
            x.WasFirst = true;
            x.FunctionState = functionState;
        });
        return firstWait;
    }


    //public async Task DeactivateFirstWait(int functionId)
    //{
    //    await _backgroundJobExecutor.Execute(
    //        $"DeactivateFirstWait_{functionId}",
    //        async () =>
    //        {
    //            var firstWaits = await _context
    //                    .Waits
    //                    .Include(x => x.FunctionState)
    //                    .Where(
    //                        wait =>
    //                        wait.RequestedByFunctionId == functionId &&
    //                        wait.IsFirst &&
    //                        wait.Status == WaitStatus.Waiting)
    //                    .ToListAsync();

    //            foreach (var firstWait in firstWaits)
    //            {
    //                if (firstWait != default)
    //                {
    //                    firstWait.IsFirst = false;
    //                    firstWait.Cancel();
    //                    _context.Waits.Remove(firstWait);
    //                    _context.FunctionStates.Remove(firstWait.FunctionState);
    //                }
    //                await _context.SaveChangesAsync();
    //            }
    //        },
    //        $"Error when try to deactivate first wait for function `{functionId}`.", true);
    //}
}
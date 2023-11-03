using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.ComponentModel;
using System.Reflection;

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

    public async Task<MethodWaitEntity> CloneFirstWait(MethodWaitEntity firstMatchedMethodWait)
    {
        var rootId = int.Parse(firstMatchedMethodWait.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)[0]);
        var resumableFunction =
            rootId != firstMatchedMethodWait.Id ?
            await _waitsRepository.GetMethodInfoForRf(rootId) :
            firstMatchedMethodWait.RequestedByFunction.MethodInfo;

        try
        {
            var firstWaitClone = await GetFirstWait(resumableFunction, false);
            firstWaitClone.Status = WaitStatus.Temp;
            firstWaitClone.ActionOnChildrenTree(waitClone =>
            {
                waitClone.IsFirst = false;
                waitClone.WasFirst = true;
                waitClone.FunctionState.StateObject = firstMatchedMethodWait?.FunctionState?.StateObject;
                if (waitClone is TimeWaitEntity timeWait)
                {
                    timeWait.TimeWaitMethod.ExtraData.JobId = _backgroundJobClient.Schedule(
                        () => new LocalRegisteredMethods().TimeWait(
                        new TimeWaitInput
                        {
                            TimeMatchId = firstMatchedMethodWait.MandatoryPart,
                            RequestedByFunctionId = firstMatchedMethodWait.RequestedByFunctionId,
                            Description = $"[{timeWait.Name}] in function [{firstMatchedMethodWait.RequestedByFunction.RF_MethodUrn}:{firstMatchedMethodWait.FunctionState.Id}]"//Todo:bug: not same description as first wait
                        }), timeWait.TimeToWait);
                    timeWait.TimeWaitMethod.MandatoryPart = firstMatchedMethodWait.MandatoryPart;
                    timeWait.IgnoreJobCreation = true;
                }

            });

            firstWaitClone.FunctionState.Logs.AddRange(firstWaitClone.FunctionState.Logs);
            firstWaitClone.FunctionState.Status =
                firstWaitClone.FunctionState.HasErrors() ?
                FunctionInstanceStatus.InError :
                FunctionInstanceStatus.InProgress;
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
            await _serviceRepo.AddErrorLog(ex, error, StatusCodes.FirstWait);
            throw new Exception(error, ex);
        }
    }

    [DisplayName("Register First Wait for Function [{0}]")]
    public async Task RegisterFirstWait(int functionId)
    {
        MethodInfo resumableFunction = null;
        var functionName = "";
        await _backgroundJobExecutor.Execute(
            $"FirstWaitProcessor_RegisterFirstWait_{functionId}",
            async () =>
            {
                try
                {
                    var resumableFunctionId = await _methodIdentifierRepo.GetResumableFunction(functionId);
                    resumableFunction = resumableFunctionId.MethodInfo;
                    functionName = resumableFunction.Name;
                    _logger.LogInformation($"Trying Start Resumable Function [{resumableFunctionId.RF_MethodUrn}] And Register First Wait");
                    var firstWait = await GetFirstWait(resumableFunction, true);

                    if (firstWait != null)
                    {
                        await _serviceRepo.AddLog(
                            $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.", LogType.Info, StatusCodes.FirstWait);

                        await _waitsRepository.SaveWait(firstWait);
                        _logger.LogInformation(
                            $"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (resumableFunction != null)
                        await _serviceRepo.AddErrorLog(ex, ErrorMsg(), StatusCodes.FirstWait);

                    await _waitsRepository.RemoveFirstWaitIfExist(functionId);
                    throw;
                }

            },
            ErrorMsg(), true);
        string ErrorMsg() => $"Error when try to register first wait for function [{functionName}:{functionId}]";
    }


    public async Task<WaitEntity> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
    {
        try
        {
            //todo: ResumableFunctionsContainer must be constructor less if you want to pass dependancies create a method `SetDependencies`
            var classInstance = (ResumableFunctionsContainer)Activator.CreateInstance(resumableFunction.DeclaringType);

            if (classInstance == null)
            {
                var errorMsg = $"Can't initiate a new instance of [{resumableFunction.DeclaringType.FullName}]";
                await _serviceRepo.AddErrorLog(null, errorMsg, StatusCodes.FirstWait);

                throw new NullReferenceException(errorMsg);
            }

            classInstance.InitializeDependencies(_serviceProvider);
            classInstance.CurrentResumableFunction = resumableFunction;
            var functionRunner = new FunctionRunner(classInstance, resumableFunction);
            if (functionRunner.ResumableFunctionExistInCode is false)
            {
                var message = $"Resumable function ({resumableFunction.GetFullName()}) not exist in code.";
                _logger.LogWarning(message);
                await _serviceRepo.AddErrorLog(null, message, StatusCodes.FirstWait);

                throw new NullReferenceException(message);
            }

            await functionRunner.MoveNextAsync();
            var firstWait = functionRunner.CurrentWait;

            if (firstWait == null)
            {
                await _serviceRepo.AddErrorLog(
                    null, 
                    $"Can't get first wait in function [{resumableFunction.GetFullName()}].",
                    StatusCodes.FirstWait);
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
            firstWait.ActionOnChildrenTree(x =>
            {
                x.RequestedByFunction = methodId;
                x.RequestedByFunctionId = methodId.Id;
                x.IsFirst = true;
                x.WasFirst = true;
                x.FunctionState = functionState;
            });
            return firstWait;
        }
        catch (Exception ex)
        {
            await _serviceRepo.AddErrorLog(ex, "Error when get first wait.", StatusCodes.FirstWait);
            throw;
        }
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
    //        $"Error when try to deactivate first wait for function [{functionId}].", true);
    //}
}
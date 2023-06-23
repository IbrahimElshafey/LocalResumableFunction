﻿using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;

internal class FirstWaitProcessor : IFirstWaitProcessor
{
    private readonly ILogger<FirstWaitProcessor> _logger;
    private readonly FunctionDataContext _context;
    private readonly IMethodIdsRepo _methodIdentifierRepo;
    private readonly IWaitsRepo _waitsRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IBackgroundProcess _backgroundJobClient;

    public FirstWaitProcessor(
        ILogger<FirstWaitProcessor> logger,
        FunctionDataContext context,
        IServiceProvider serviceProvider,
        IMethodIdsRepo methodIdentifierRepo,
        IWaitsRepo waitsRepository,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        IBackgroundProcess backgroundJobClient)
    {
        _logger = logger;
        _context = context;
        _serviceProvider = serviceProvider;
        _methodIdentifierRepo = methodIdentifierRepo;
        _waitsRepository = waitsRepository;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait)
    {
        var resumableFunction = firstMatchedMethodWait.RequestedByFunction.MethodInfo;

        try
        {
            var firstWaitClone = await GetFirstWait(resumableFunction, false);
            firstWaitClone.Status = WaitStatus.Temp;
            firstWaitClone.ActionOnWaitsTree(wait =>
            {
                wait.IsFirst = false;
                wait.WasFirst = true;
                wait.FunctionState.StateObject = firstMatchedMethodWait?.FunctionState?.StateObject;
                if (wait is TimeWait timeWait)
                {
                    timeWait.TimeWaitMethod.ExtraData.JobId = _backgroundJobClient.Schedule(
                        () => new LocalRegisteredMethods().TimeWait(
                        new TimeWaitInput
                        {
                            TimeMatchId = firstMatchedMethodWait.MandatoryPart.Substring(1)
                        }), timeWait.TimeToWait);
                    timeWait.TimeWaitMethod.MandatoryPart = firstMatchedMethodWait.MandatoryPart;
                    timeWait.IgnoreJobCreation = true;
                }

            });
            //firstWaitClone.FunctionState.AddLog(
            //    $"[{resumableFunction.GetFullName()}] started and wait [{firstMatchedMethodWait.Name}] to match.", LogType.Info);

            firstWaitClone.FunctionState.Logs.AddRange(firstWaitClone.FunctionState.Logs);
            firstWaitClone.FunctionState.Status =
                firstWaitClone.FunctionState.HasErrors() ?
                FunctionStatus.Error :
                FunctionStatus.InProgress;
            await _waitsRepository.SaveWait(firstWaitClone);//first wait clone

            var currentMw = firstWaitClone.GetChildMethodWait(firstMatchedMethodWait.Name);
            currentMw.Status = WaitStatus.Waiting;
            currentMw.Input = firstMatchedMethodWait.Input;
            currentMw.Output = firstMatchedMethodWait.Output;
            var waitTemplate = await _context
                .WaitTemplates
                .Select(WaitTemplate.BasicMatchSelector)
                .FirstAsync(x => x.Id == firstMatchedMethodWait.TemplateId);
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
            await LogErrorToService(ex, error);
            throw new Exception(error, ex);
        }
    }

    public async Task RegisterFirstWait(int functionId)
    {
        MethodInfo resumableFunction = null;
        string errorMsg = $"Error when try to register first wait for function [{functionId}]";
        await _backgroundJobExecutor.Execute(
            $"FirstWaitProcessor_RegisterFirstWait_{functionId}",
            async () =>
            {
                try
                {
                    var resumableFunctionId = await _methodIdentifierRepo.GetResumableFunction(functionId);
                    resumableFunction = resumableFunctionId.MethodInfo;
                    WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
                    var firstWait = await GetFirstWait(resumableFunction, true);
                    if (firstWait != null)
                        firstWait.FunctionState.AddLog(
                            $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.");
                    await _waitsRepository.SaveWait(firstWait);//first wait when register function
                    WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (resumableFunction != null)
                        await LogErrorToService(ex, errorMsg);
                    await _waitsRepository.RemoveFirstWaitIfExist(functionId);
                    throw;
                }

            },
            errorMsg, true);
    }

    //todo:review this method
    private async Task LogErrorToService(Exception ex, string errorMsg)
    {
        _logger.LogError(ex, errorMsg);
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = errorMsg + ex,
            Type = LogType.Error
        });
        await _context.SaveChangesAsync();
    }

    public async Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
    {
        var classInstance = (ResumableFunction)Activator.CreateInstance(resumableFunction.DeclaringType);

        if (classInstance == null)
        {

            var errorMsg = $"Can't initiate a new instance of [{resumableFunction.DeclaringType.FullName}]";
            await LogErrorToService(null, errorMsg);
            throw new NullReferenceException(errorMsg);
        }

        classInstance.InitializeDependencies(_serviceProvider);
        classInstance.CurrentResumableFunction = resumableFunction;
        var functionRunner = new FunctionRunner(classInstance, resumableFunction);
        if (functionRunner.ResumableFunctionExistInCode is false)
        {
            string message = $"Resumable function ({resumableFunction.GetFullName()}) not exist in code.";
            _logger.LogWarning(message);
            await LogErrorToService(null, message);
            throw new NullReferenceException(message);
        }

        await functionRunner.MoveNextAsync();
        var firstWait = functionRunner.Current;
        var methodId = await _methodIdentifierRepo.GetResumableFunction(new MethodData(resumableFunction));
        if (removeIfExist)
        {
            WriteMessage("First wait already exist it will be deleted and recreated since it may be changed.");
            await _waitsRepository.RemoveFirstWaitIfExist(methodId.Id);
        }
        var functionState = new ResumableFunctionState
        {
            ResumableFunctionIdentifier = methodId,
            StateObject = classInstance,
            ServiceId = _settings.CurrentServiceId,
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

    private void WriteMessage(string message)
    {
        _logger.LogInformation(message);
    }

    public async Task DeactivateFirstWait(int functionId)
    {
        await _backgroundJobExecutor.Execute(
            $"DeactivateFirstWait_{functionId}",
            async () =>
            {
                var firstWaits = await _context
                        .Waits
                        .Include(x => x.FunctionState)
                        .Where(wait =>
                                wait.RequestedByFunctionId == functionId &&
                                wait.IsFirst &&
                                wait.Status == WaitStatus.Waiting)
                        .ToListAsync();

                foreach (var firstWait in firstWaits)
                {
                    if (firstWait != default)
                    {
                        firstWait.IsFirst = false;
                        firstWait.Cancel();
                        _context.Waits.Remove(firstWait);
                        _context.FunctionStates.Remove(firstWait.FunctionState);
                    }
                    await _context.SaveChangesAsync();
                }
            },
            $"Error when try to deactivate first wait for function [{functionId}]", true);
    }
}
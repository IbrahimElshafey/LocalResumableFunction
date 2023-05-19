using System.Linq.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Data;

namespace ResumableFunctions.Handler;

public class SaveWaitHandler : ISaveWaitHandler
{
    private readonly ILogger<SaveWaitHandler> _logger;
    private readonly FunctionDataContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SaveWaitHandler(ILogger<SaveWaitHandler> logger, FunctionDataContext context, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<bool> SaveWaitRequestToDb(Wait newWait)
    {
        if (newWait.IsValidWaitRequest() is false)
        {
            string message =
                $"Error when validate the requested wait [{newWait.Name}] " +
                $"that requested by function [{newWait?.RequestedByFunction}].";
            _logger.LogError(message);
        }
        switch (newWait)
        {
            case MethodWait methodWait:
                await MethodWaitRequested(methodWait);
                break;
            case WaitsGroup manyWaits:
                await WaitsGroupRequested(manyWaits);
                break;
            case FunctionWait functionWait:
                await FunctionWaitRequested(functionWait);
                break;
            case TimeWait timeWait:
                await TimeWaitRequested(timeWait);
                break;
            case ReplayRequest replayWait:
                await ReplayWait(replayWait);
                break;
        }

        return false;
    }



    private async Task MethodWaitRequested(MethodWait methodWait)
    {
        var methodToWait = await _context.methodIdentifierRepo.GetWaitMethod(methodWait);

        methodWait.MethodToWait = methodToWait;
        methodWait.MethodToWaitId = methodToWait.Id;
        methodWait.MethodGroupToWait = methodToWait.ParentMethodGroup;
        methodWait.MethodGroupToWaitId = methodToWait.ParentMethodGroupId;
        methodWait.RewriteExpressions();

        await _context.waitsRepository.AddWait(methodWait);
    }

    private async Task WaitsGroupRequested(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var waitGroupChild = manyWaits.ChildWaits[index];
            waitGroupChild.FunctionState = manyWaits.FunctionState;
            waitGroupChild.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            waitGroupChild.RequestedByFunction = manyWaits.RequestedByFunction;
            waitGroupChild.StateAfterWait = manyWaits.StateAfterWait;
            waitGroupChild.ParentWait = manyWaits;
            await SaveWaitRequestToDb(waitGroupChild);//child wait in group
        }

        await _context.waitsRepository.AddWait(manyWaits);
    }

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        await _context.waitsRepository.AddWait(functionWait);

        var functionRunner = new FunctionRunner(functionWait.CurrentFunction, functionWait.FunctionInfo);
        var hasNext = await functionRunner.MoveNextAsync();
        functionWait.FirstWait = functionRunner.Current;
        if (hasNext is false)
        {
            _logger.LogWarning($"No waits exist in sub function ({functionWait.FunctionInfo.GetFullName()})");
            return;
        }

        functionWait.FirstWait = functionRunner.Current;
        //functionWait.FirstWait.StateAfterWait = functionRunner.GetState();
        functionWait.FirstWait.FunctionState = functionWait.FunctionState;
        functionWait.FirstWait.FunctionStateId = functionWait.FunctionState.Id;
        functionWait.FirstWait.ParentWait = functionWait;
        functionWait.FirstWait.ParentWaitId = functionWait.Id;
        var methodId = await _context.methodIdentifierRepo.GetResumableFunction(new MethodData(functionWait.FunctionInfo));
        functionWait.FirstWait.RequestedByFunction = methodId;
        functionWait.FirstWait.RequestedByFunctionId = methodId.Id;

        if (functionWait.FirstWait is ReplayRequest)
        {
            _logger.LogWarning("First wait can't be a replay request");
            //await ReplayWait(replayWait);//todo:review first wait is replay for what??
        }
        else
            await SaveWaitRequestToDb(functionWait.FirstWait);//first wait for sub function
    }

    private async Task TimeWaitRequested(TimeWait timeWait)
    {
        var timeWaitMethod = new MethodWait<string, string>(new LocalRegisteredMethods().TimeWait);
        var functionType = typeof(Func<,,>)
            .MakeGenericType(
                typeof(string),
                typeof(string),
                typeof(bool));
        var inputParameter = Expression.Parameter(typeof(string), "input");
        var outputParameter = Expression.Parameter(typeof(string), "output");
        timeWaitMethod.SetDataExpression = Expression.Lambda(
            functionType,
            timeWait.SetDataExpression.Body,
            inputParameter,
            outputParameter);
        timeWaitMethod.MatchIfExpression = Expression.Lambda(
            functionType,
            Expression.Equal(inputParameter, Expression.Constant(timeWait.UniqueMatchId)),
            inputParameter,
            outputParameter);
        timeWaitMethod.CurrentFunction = timeWait.CurrentFunction;

        var jobId = _backgroundJobClient.Schedule(() => new LocalRegisteredMethods().TimeWait(timeWait.UniqueMatchId), timeWait.TimeToWait);
        _context.Entry(timeWait).State = EntityState.Detached;
        timeWaitMethod.ParentWait = timeWait.ParentWait;
        timeWaitMethod.FunctionState = timeWait.FunctionState;
        timeWaitMethod.RequestedByFunctionId = timeWait.RequestedByFunctionId;
        timeWaitMethod.StateBeforeWait = timeWait.StateBeforeWait;
        timeWaitMethod.StateAfterWait = timeWait.StateAfterWait;
        timeWaitMethod.ExtraData =
            new TimeWaitData
            {
                TimeToWait = timeWait.TimeToWait,
                UniqueMatchId = timeWait.UniqueMatchId,
                JobId = jobId,
            };
        timeWaitMethod.RefineMatchModifier = timeWait.UniqueMatchId;
        await MethodWaitRequested(timeWaitMethod);
    }
}
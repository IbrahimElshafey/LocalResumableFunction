using System.Linq.Expressions;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler
{
    internal async Task<bool> SaveWaitRequestToDb(Wait newWait)
    {
        newWait.Status = WaitStatus.Waiting;
        if (Validate(newWait) is false) return false;
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
        var methodId = methodWait.MethodData != null
            ? await _metodIdsRepo.GetMethodIdentifierFromDb(methodWait.MethodData)
            : await _context.MethodIdentifiers.FindAsync(methodWait.WaitMethodIdentifierId);
        methodWait.WaitMethodIdentifier = methodId;
        methodWait.WaitMethodIdentifierId = methodId.Id;
        methodWait.RewriteExpressions();

        await _waitsRepository.AddWait(methodWait);
    }

    private async Task WaitsGroupRequested(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var waitGroupChild = manyWaits.ChildWaits[index];
            waitGroupChild.Status = WaitStatus.Waiting;
            waitGroupChild.FunctionState = manyWaits.FunctionState;
            waitGroupChild.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            waitGroupChild.RequestedByFunction = manyWaits.RequestedByFunction;
            waitGroupChild.StateAfterWait = manyWaits.StateAfterWait;
            waitGroupChild.ParentWait = manyWaits;
            await SaveWaitRequestToDb(waitGroupChild);
        }

        await _waitsRepository.AddWait(manyWaits);
    }

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        await _waitsRepository.AddWait(functionWait);
        //await _context.SaveChangesAsync();

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
        var methodId = await _metodIdsRepo.GetMethodIdentifierFromDb(new MethodData(functionWait.FunctionInfo));
        functionWait.FirstWait.RequestedByFunction = methodId;
        functionWait.FirstWait.RequestedByFunctionId = methodId.Id;

        if (functionWait.FirstWait is ReplayRequest)
        {
            _logger.LogWarning("First wait can't be a replay request");
            //await ReplayWait(replayWait);//todo:review first wait is replay for what??
        }
        else
            await SaveWaitRequestToDb(functionWait.FirstWait);
    }

    private async Task TimeWaitRequested(TimeWait timeWait)
    {
        var methodWait = new MethodWait<string, string>(new LocalRegisteredMethods().TimeWait);
        var functionType = typeof(Func<,,>)
            .MakeGenericType(
                typeof(string),
                typeof(string),
                typeof(bool));
        var inputParameter = Expression.Parameter(typeof(string), "input");
        var outputParameter = Expression.Parameter(typeof(string), "output");
        methodWait.SetDataExpression = Expression.Lambda(
            functionType,
            timeWait.SetDataExpression.Body,
            inputParameter,
            outputParameter);
        methodWait.MatchIfExpression = Expression.Lambda(
            functionType,
            Expression.Equal(outputParameter, Expression.Constant(timeWait.UniqueMatchId)),
            inputParameter,
            outputParameter);
        methodWait.CurrentFunction = timeWait.CurrentFunction;

        var jobId = _backgroundJobClient.Schedule(() => new LocalRegisteredMethods().TimeWait(timeWait.UniqueMatchId), timeWait.TimeToWait);
        //_context.Waits.Remove(timeWait);
        _context.Entry(timeWait).State = EntityState.Detached;
        methodWait.ParentWait = timeWait.ParentWait;
        methodWait.FunctionState = timeWait.FunctionState;
        methodWait.RequestedByFunctionId = timeWait.RequestedByFunctionId;
        methodWait.StateBeforeWait = timeWait.StateBeforeWait;
        methodWait.StateAfterWait = timeWait.StateAfterWait;
        methodWait.ExtraData = new TimeWaitData { TimeToWait = timeWait.TimeToWait, UniqueMatchId = timeWait.UniqueMatchId, JobId = jobId };
        await MethodWaitRequested(methodWait);
    }

    private bool Validate(Wait nextWait)
    {
        return true;
    }


}
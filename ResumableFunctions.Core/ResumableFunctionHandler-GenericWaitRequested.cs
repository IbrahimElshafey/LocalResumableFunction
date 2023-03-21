using System.Linq.Expressions;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace ResumableFunctions.Core;

internal partial class ResumableFunctionHandler
{
    internal async Task<bool> GenericWaitRequested(Wait newWait)
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
        methodWait.SetExpressions();

        await _waitsRepository.AddWait(methodWait);
    }

    private async Task WaitsGroupRequested(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var wait = manyWaits.ChildWaits[index];
            wait.Status = WaitStatus.Waiting;
            wait.FunctionState = manyWaits.FunctionState;
            wait.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            wait.RequestedByFunction = manyWaits.RequestedByFunction;
            wait.StateAfterWait = manyWaits.StateAfterWait;
            wait.ParentWait = manyWaits;
            await GenericWaitRequested(wait);
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
            WriteMessage($"No waits exist in sub function ({functionWait.FunctionInfo.Name})");
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

        if (functionWait.FirstWait is ReplayRequest replayWait)
        {
            WriteMessage("First wait can't be replay wait");
            //await ReplayWait(replayWait);//todo:review first wait is replay for what??
        }
        else
            await GenericWaitRequested(functionWait.FirstWait);
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

        _backgroundJobClient.Schedule(() => new LocalRegisteredMethods().TimeWait(timeWait.UniqueMatchId), timeWait.TimeToWait);
        //_context.Waits.Remove(timeWait);
        _context.Entry(timeWait).State = EntityState.Detached;
        methodWait.ParentWait = timeWait.ParentWait;
        methodWait.FunctionState = timeWait.FunctionState;
        methodWait.RequestedByFunctionId = timeWait.RequestedByFunctionId;
        methodWait.StateBeforeWait = timeWait.StateBeforeWait;
        methodWait.StateAfterWait = timeWait.StateAfterWait;
        methodWait.ExtraData = new { timeWait.TimeToWait, timeWait.UniqueMatchId };
        await MethodWaitRequested(methodWait);
    }

    private bool Validate(Wait nextWait)
    {
        return true;
    }
}
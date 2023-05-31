using System.Diagnostics;
using System.Linq;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using System.Linq.Expressions;
using System;
using ResumableFunctions.Handler.Core;

namespace ResumableFunctions.Handler.DataAccess;

internal partial class WaitsRepository : IWaitsRepository
{
    public async Task<bool> SaveWaitRequestToDb(Wait newWait)
    {
        newWait.ServiceId = _settings.CurrentServiceId;
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
        }

        return false;
    }



    private async Task MethodWaitRequested(MethodWait methodWait)
    {
        var methodToWait = await _methodIdentifierRepo.GetWaitMethod(methodWait);

        methodWait.MethodToWait = methodToWait;
        methodWait.ServiceId = _settings.CurrentServiceId; ;
        methodWait.MethodToWaitId = methodToWait.Id;
        methodWait.MethodGroupToWait = methodToWait.ParentMethodGroup;
        methodWait.MethodGroupToWaitId = methodToWait.ParentMethodGroupId;
        methodWait.RewriteExpressions();

        await AddWait(methodWait);
    }

    private async Task WaitsGroupRequested(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var childWait = manyWaits.ChildWaits[index];
            childWait.FunctionState = manyWaits.FunctionState;
            childWait.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            childWait.RequestedByFunction = manyWaits.RequestedByFunction;
            childWait.StateAfterWait = manyWaits.StateAfterWait;
            childWait.ParentWait = manyWaits;
            childWait.ServiceId = _settings.CurrentServiceId;
            await SaveWaitRequestToDb(childWait);//child wait in group
        }

        await AddWait(manyWaits);
    }

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        await AddWait(functionWait);

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
        functionWait.FirstWait.ServiceId = _settings.CurrentServiceId;
        var methodId = await _methodIdentifierRepo.GetResumableFunction(new MethodData(functionWait.FunctionInfo));
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
        //todo: revisit after ComputedInstanceId done, no need for 
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

    public Task AddWait(Wait wait)
    {
        var isExistLocal = _context.Waits.Local.Contains(wait);
        var notAddStatus = _context.Entry(wait).State != EntityState.Added;
        if (isExistLocal || !notAddStatus) return Task.CompletedTask;

        Console.WriteLine($"==> Add Wait [{wait.Name}] with type [{wait.WaitType}]");
        if (wait is WaitsGroup waitGroup)
        {
            waitGroup.ChildWaits.RemoveAll(x => x is TimeWait);
        }
        if (wait is MethodWait { MethodToWaitId: > 0 } methodWait)
        {
            //does this may cause a problem
            methodWait.MethodToWait = null;
        }
        _context.Waits.Add(wait);
        return Task.CompletedTask;
    }
}
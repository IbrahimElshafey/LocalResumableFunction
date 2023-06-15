using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Helpers;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using System.Linq.Expressions;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers.Expressions;
using FastExpressionCompiler;

namespace ResumableFunctions.Handler.DataAccess;

internal partial class WaitsRepo : IWaitsRepo
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

        await _context.SaveChangesAsync();
        return false;
    }



    private async Task MethodWaitRequested(MethodWait methodWait)
    {
        var methodId = await _methodIdsRepo.GetId(methodWait);
        var funcId = methodWait.RequestedByFunctionId;
        var waitExpressionsHash = new WaitExpressionsHash(methodWait.MatchExpression, methodWait.SetDataExpression);
        var expressionsHash = waitExpressionsHash.Hash;
        var waitTemplate =
            await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, methodId.GroupId) ??
            await _waitTemplatesRepo.AddNewTemplate(waitExpressionsHash, methodWait.CurrentFunction, funcId, methodId.GroupId, methodId.MethodId);
        methodWait.ServiceId = _settings.CurrentServiceId;
        methodWait.MethodToWaitId = methodId.MethodId;
        methodWait.MethodGroupToWaitId = methodId.GroupId;
        methodWait.TemplateId = waitTemplate.Id;
        if (waitTemplate.InstanceMandatoryPartExpression != null)
        {
            var partIdFunc = waitTemplate.InstanceMandatoryPartExpression.CompileFast();
            var parts = (string[])partIdFunc.DynamicInvoke(methodWait.CurrentFunction);
            if (parts?.Any() == true)
                methodWait.MandatoryPart = string.Join("#", parts);
        }
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
            childWait.CurrentFunction = manyWaits.CurrentFunction;
            childWait.ParentWait = manyWaits;
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
        var methodId = await _methodIdsRepo.GetResumableFunction(new MethodData(functionWait.FunctionInfo));
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

        var timeWaitMethod = new MethodWait<string, bool>(typeof(LocalRegisteredMethods).GetMethod("TimeWait"));
        var functionType = typeof(Func<,,>)
            .MakeGenericType(
                typeof(string),
                typeof(bool),
                typeof(bool));
        //todo: revisit after ComputedInstanceId done, no need for 
        var inputParameter = Expression.Parameter(typeof(string), "input");
        var outputParameter = Expression.Parameter(typeof(bool), "output");
        var setDataExpression = Expression.Lambda(
            functionType,
            timeWait.SetDataExpression.Body,
            inputParameter,
            outputParameter);
        var matchExpression = Expression.Lambda(
            functionType,
            Expression.Equal(inputParameter, Expression.Constant(timeWait.UniqueMatchId)),
            inputParameter,
            outputParameter);

        timeWaitMethod
           .SetData((Expression<Func<string, bool, bool>>)setDataExpression)
           .MatchIf((Expression<Func<string, bool, bool>>)matchExpression);
        timeWaitMethod.CurrentFunction = timeWait.CurrentFunction;

        LocalRegisteredMethods localMethods = null;
        var jobId = _backgroundJobClient.Schedule(() => localMethods.TimeWait(timeWait.UniqueMatchId), timeWait.TimeToWait);

        _context.Entry(timeWait).State = EntityState.Detached;
        timeWaitMethod.ParentWait = timeWait.ParentWait;
        timeWaitMethod.FunctionState = timeWait.FunctionState;
        timeWaitMethod.RequestedByFunctionId = timeWait.RequestedByFunctionId;
        timeWaitMethod.StateBeforeWait = timeWait.StateBeforeWait;
        timeWaitMethod.StateAfterWait = timeWait.StateAfterWait;
        timeWaitMethod.ExtraData =
            new WaitExtraData
            {
                TimeToWait = timeWait.TimeToWait,
                UniqueMatchId = timeWait.UniqueMatchId,
                JobId = jobId,
            };
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
            //Bug:does this may cause a problem
            methodWait.MethodToWait = null;
        }
        _context.Waits.Add(wait);
        return Task.CompletedTask;
    }
}
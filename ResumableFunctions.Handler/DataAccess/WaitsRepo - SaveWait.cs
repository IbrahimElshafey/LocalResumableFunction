using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal partial class WaitsRepo
{
    public async Task<bool> SaveWait(Wait newWait)
    {
        if (newWait.IsValidWaitRequest() is false)
        {
            var message =
                $"Error when validate the requested wait [{newWait.Name}] " +
                $"that requested by function [{newWait.RequestedByFunction}].";
            _logger.LogError(message);
        }

        switch (newWait)
        {
            case MethodWait methodWait:
                await SaveMethodWait(methodWait);
                break;
            case WaitsGroup manyWaits:
                await SaveWaitsGroup(manyWaits);
                break;
            case FunctionWait functionWait:
                await SaveFunctionWait(functionWait);
                break;
            case TimeWait timeWait:
                await HandleTimeWaitRequest(timeWait);
                break;
        }

        return false;
    }

    public async Task<MethodWait> GetMethodWait(int waitId, params Expression<Func<MethodWait, object>>[] includes)
    {
        var query = _context.MethodWaits.AsQueryable();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return await query
            .Where(x => x.Status == WaitStatus.Waiting)
            .FirstOrDefaultAsync(x => x.Id == waitId);
    }

    public async Task<MethodInfo> GetRequestedByMethodInfo(int rootId)
    {
        return (await _context.Waits.Include(x => x.RequestedByFunction).FirstAsync(x => x.Id == rootId))
            .RequestedByFunction.MethodInfo;
    }

    private async Task SaveMethodWait(MethodWait methodWait)
    {
        var methodId = await _methodIdsRepo.GetId(methodWait);
        var funcId = methodWait.RequestedByFunctionId;
        var waitExpressionsHash = new WaitExpressionsHash(methodWait.MatchExpression, methodWait.SetDataExpression);
        var expressionsHash = waitExpressionsHash.Hash;
        await SetWaitTemplate();
        methodWait.MethodToWaitId = methodId.MethodId;
        methodWait.MethodGroupToWaitId = methodId.GroupId;

        await AddWait(methodWait);

        async Task SetWaitTemplate()
        {
            WaitTemplate waitTemplate = null;
            if (methodWait.TemplateId == default)
            {
                waitTemplate =
                    await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, methodId.GroupId) ??
                    await _waitTemplatesRepo.AddNewTemplate(waitExpressionsHash, methodWait.CurrentFunction, funcId,
                        methodId.GroupId, methodId.MethodId);
            }
            else
            {
                waitTemplate = await _waitTemplatesRepo.GetById(methodWait.TemplateId);
            }
            methodWait.TemplateId = waitTemplate.Id;
            methodWait.Template = waitTemplate;
            if (waitTemplate.InstanceMandatoryPartExpression != null)
            {
                var partIdFunc = waitTemplate.InstanceMandatoryPartExpression.CompileFast();
                var parts = (object[])partIdFunc.DynamicInvoke(methodWait.CurrentFunction);
                if (parts?.Any() == true)
                    methodWait.MandatoryPart = string.Join("#", parts);
            }
        }
    }



    private async Task SaveWaitsGroup(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var childWait = manyWaits.ChildWaits[index];
            childWait.FunctionState = manyWaits.FunctionState;
            childWait.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            childWait.RequestedByFunction = manyWaits.RequestedByFunction;
            childWait.StateAfterWait = manyWaits.StateAfterWait;
            childWait.ParentWait = manyWaits;
            childWait.CurrentFunction = manyWaits.CurrentFunction;
            await SaveWait(childWait);
        }

        await AddWait(manyWaits);
    }

    private async Task SaveFunctionWait(FunctionWait functionWait)
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
        functionWait.FirstWait.FunctionState = functionWait.FunctionState;
        functionWait.FirstWait.FunctionStateId = functionWait.FunctionState.Id;
        functionWait.FirstWait.ParentWait = functionWait;
        functionWait.FirstWait.ParentWaitId = functionWait.Id;
        functionWait.FirstWait.IsFirst = functionWait.IsFirst;
        var methodId = await _methodIdsRepo.GetResumableFunction(new MethodData(functionWait.FunctionInfo));
        functionWait.FirstWait.RequestedByFunction = methodId;
        functionWait.FirstWait.RequestedByFunctionId = methodId.Id;

        if (functionWait.FirstWait is ReplayRequest)
        {
            _logger.LogWarning("First wait can't be a replay request");
            //await ReplayWait(replayWait);//todo:review first wait is replay for what??
        }
        else
            await SaveWait(functionWait.FirstWait);//first wait for sub function
    }

    private async Task HandleTimeWaitRequest(TimeWait timeWait)
    {
        var timeWaitMethod = timeWait.TimeWaitMethod;

        var methodId = await _methodIdsRepo.GetId(timeWaitMethod);

        var timeWaitInput = new TimeWaitInput
        {
            TimeMatchId = timeWait.UniqueMatchId,
            Description = $"`{timeWait.Name}` in function `{timeWait.RequestedByFunction.RF_MethodUrn}:{timeWait.FunctionState.Id}`"
        };
        if (!timeWait.IgnoreJobCreation)
            timeWaitMethod.ExtraData.JobId = _backgroundJobClient.Schedule(
                () => new LocalRegisteredMethods().TimeWait(timeWaitInput), timeWait.TimeToWait);

        timeWaitMethod.MethodToWaitId = methodId.MethodId;
        timeWaitMethod.MethodGroupToWaitId = methodId.GroupId;

        await SaveMethodWait(timeWaitMethod);
        timeWaitMethod.MandatoryPart = timeWait.UniqueMatchId;
        _context.Entry(timeWait).State = EntityState.Detached;
    }



    public Task AddWait(Wait wait)
    {
        var isExistLocal = _context.Waits.Local.Contains(wait);
        var notAddStatus = _context.Entry(wait).State != EntityState.Added;
        SetNodeType(wait);
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

    private void SetNodeType(Wait wait)
    {
        wait.ActionOnWaitsTree(w => w.IsRootNode = w.ParentWait == null && w.ParentWaitId == null);
    }
}
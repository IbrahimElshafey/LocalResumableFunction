using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace ResumableFunctions.Handler.DataAccess;

internal partial class WaitsRepo
{
    public async Task<bool> SaveWait(WaitEntity newWait)
    {
        if (newWait.ValidateWaitRequest() is false)
        {
            var message =
                $"Error when validate the requested wait [{newWait.Name}] " +
                $"that requested by function [{newWait.RequestedByFunction}].";
            _logger.LogError(message);
        }

        switch (newWait)
        {
            case MethodWaitEntity methodWait:
                await SaveMethodWait(methodWait);
                break;
            case WaitsGroupEntity manyWaits:
                await SaveWaitsGroup(manyWaits);
                break;
            case FunctionWaitEntity functionWait:
                await SaveFunctionWait(functionWait);
                break;
            case TimeWaitEntity timeWait:
                await HandleTimeWaitRequest(timeWait);
                break;
        }

        return false;
    }

    public async Task<MethodWaitEntity> GetMethodWait(int waitId, params Expression<Func<MethodWaitEntity, object>>[] includes)
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

    public async Task<MethodInfo> GetRequestedByMethodInfo(int waitId)
    {
        return (await _context.Waits.Include(x => x.RequestedByFunction).FirstAsync(x => x.Id == waitId))
            .RequestedByFunction.MethodInfo;
    }

    private async Task SaveMethodWait(MethodWaitEntity methodWait)
    {
        var methodId = await _methodIdsRepo.GetId(methodWait);
        var funcId = methodWait.RequestedByFunctionId;
        var expressionsHash = new ExpressionsHashCalculator(methodWait.MatchExpression, methodWait.AfterMatchAction, methodWait.CancelMethodAction).GetHash();
        methodWait.MethodToWaitId = methodId.MethodId;
        methodWait.MethodGroupToWaitId = methodId.GroupId;

        await SetWaitTemplate();
        await AddWait(methodWait);

        async Task SetWaitTemplate()
        {
            WaitTemplate waitTemplate;
            if (methodWait.TemplateId == default)
            {
                waitTemplate =
                    await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, methodId.GroupId) ??
                    await _waitTemplatesRepo.AddNewTemplate(expressionsHash, methodWait);
            }
            else
            {
                waitTemplate = await _waitTemplatesRepo.GetById(methodWait.TemplateId);
            }
            methodWait.TemplateId = waitTemplate.Id;
            methodWait.Template = waitTemplate;
        }
    }



    private async Task SaveWaitsGroup(WaitsGroupEntity manyWaits)
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
            //if (childWait.Closure == null)
            //    childWait.SetClosure(manyWaits.Closure);
            await SaveWait(childWait);
        }

        await AddWait(manyWaits);
    }

    private async Task SaveFunctionWait(FunctionWaitEntity functionWait)
    {
        try
        {
            await AddWait(functionWait);

            var functionRunner = new FunctionRunner(functionWait.CurrentFunction, functionWait.FunctionInfo);
            var hasNext = await functionRunner.MoveNextAsync();
            functionWait.FirstWait = functionRunner.CurrentWait;
            if (hasNext is false)
            {
                _logger.LogWarning($"No waits exist in sub function ({functionWait.FunctionInfo.GetFullName()})");
                return;
            }

            functionWait.FirstWait = functionRunner.CurrentWait;
            functionWait.FirstWait.FunctionState = functionWait.FunctionState;
            functionWait.FirstWait.FunctionStateId = functionWait.FunctionState.Id;
            functionWait.FirstWait.ParentWait = functionWait;
            functionWait.FirstWait.ParentWaitId = functionWait.Id;
            functionWait.FirstWait.IsFirst = functionWait.IsFirst;
            var methodId = await _methodIdsRepo.GetResumableFunction(new MethodData(functionWait.FunctionInfo));
            functionWait.FirstWait.RequestedByFunction = methodId;
            functionWait.FirstWait.RequestedByFunctionId = methodId.Id;

            if (functionWait.FirstWait is ReplayRequest)
                await _serviceRepo.AddErrorLog(null, "First wait can't be a replay request", StatusCodes.FirstWait);
            else
                await SaveWait(functionWait.FirstWait);//first wait for sub function
        }
        catch (Exception ex)
        {
            await _serviceRepo.AddErrorLog(ex, "When save function wait", StatusCodes.WaitValidation);
        }
    }

    private async Task HandleTimeWaitRequest(TimeWaitEntity timeWait)
    {
        var timeWaitMethod = timeWait.TimeWaitMethod;

        var methodId = await _methodIdsRepo.GetId(timeWaitMethod);

        var timeWaitInput = new TimeWaitInput
        {
            TimeMatchId = timeWait.UniqueMatchId,
            RequestedByFunctionId = timeWait.RequestedByFunctionId,
            Description = $"[{timeWait.Name}] in function [{timeWait.RequestedByFunction.RF_MethodUrn}:{timeWait.FunctionState.Id}]"
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



    public Task AddWait(WaitEntity wait)
    {
        var isExistLocal = _context.Waits.Local.Contains(wait);
        var notAddStatus = _context.Entry(wait).State != EntityState.Added;
        SetNodeType(wait);


        if (isExistLocal || !notAddStatus) return Task.CompletedTask;

        _logger.LogInformation($"Add Wait [{wait.Name}] with type [{wait.WaitType}]");
        switch (wait)
        {
            case WaitsGroupEntity waitGroup:
                waitGroup.ChildWaits.RemoveAll(x => x is TimeWaitEntity);
                break;
            case MethodWaitEntity { MethodToWaitId: > 0 } methodWait:
                //Bug:I forgot why I added this??!!
                methodWait.MethodToWait = null;
                break;
        }

        _context.Waits.Add(wait);
        return Task.CompletedTask;
    }

    private void SetNodeType(WaitEntity wait)
    {
        wait.ActionOnChildrenTree(w => w.IsRootNode = w.ParentWait == null && w.ParentWaitId == null);
    }
}
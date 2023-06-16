using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Helpers;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using System.Linq.Expressions;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers.Expressions;
using FastExpressionCompiler;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Handler.DataAccess;

internal partial class WaitsRepo
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
            var parts = (object[])partIdFunc.DynamicInvoke(methodWait.CurrentFunction);
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
            await SaveWaitRequestToDb(childWait);
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


    public Task<MethodWait> GetTimeWait(string timeWaitId)
    {
        var parts = timeWaitId.Split('#');
        var mandatoryPart = timeWaitId;
        var groupId = int.Parse(parts[1]);
        var functionId = int.Parse(parts[2]);
        var methodId = int.Parse(parts[3]);
        return _context
            .MethodWaits
            .Where(x =>
                x.MethodGroupToWaitId == groupId &&
                x.MandatoryPart == mandatoryPart &&
                x.RequestedByFunctionId == functionId &&
                x.MethodToWaitId == methodId
            )
            .FirstAsync();
    }

    private async Task TimeWaitRequested(TimeWait timeWait)
    {
        var timeWaitMethod = 
            new MethodWait<TimeWaitInput, bool>(typeof(LocalRegisteredMethods)
                .GetMethod("TimeWait"));
        TimeWaitSetDataExpression(timeWait, timeWaitMethod);
        timeWaitMethod.CurrentFunction = timeWait.CurrentFunction;

        var methodId = await _methodIdsRepo.GetId(timeWaitMethod);
        var jobId = _backgroundJobClient.Schedule(
            () =>
                new LocalRegisteredMethods().TimeWait(new TimeWaitInput
                {
                    //ServiceId = _settings.CurrentServiceId,
                    //GroupId = methodId.GroupId,
                    TimeMatchId = timeWait.UniqueMatchId,
                    //FunctionId = timeWait.RequestedByFunctionId,
                    //MethodId = methodId.MethodId
                }), timeWait.TimeToWait);
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
        timeWaitMethod.ServiceId = _settings.CurrentServiceId;
        timeWaitMethod.MethodToWaitId = methodId.MethodId;
        timeWaitMethod.MethodGroupToWaitId = methodId.GroupId;

        await MethodWaitRequested(timeWaitMethod);
        timeWaitMethod.MandatoryPart = timeWait.UniqueMatchId;
        _context.Entry(timeWait).State = EntityState.Detached;
    }

    private static void TimeWaitSetDataExpression(TimeWait timeWait, MethodWait<TimeWaitInput, bool> methodWait)
    {
        var functionType = typeof(Func<,,>)
            .MakeGenericType(
                typeof(TimeWaitInput),
                typeof(bool),
                typeof(bool));
        var inputParameter = Expression.Parameter(typeof(TimeWaitInput), "input");
        var outputParameter = Expression.Parameter(typeof(bool), "output");
        if (timeWait.SetDataExpression != null)
        {
            var setDataExpression = Expression.Lambda(
                functionType,
                timeWait.SetDataExpression.Body,
                inputParameter,
                outputParameter);
            methodWait
                .SetData((Expression<Func<TimeWaitInput, bool, bool>>)setDataExpression);
        }

        // ReSharper disable once EqualExpressionComparison
        methodWait.MatchIf((timeWaitInput, result) => timeWaitInput.TimeMatchId == "" || true);
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
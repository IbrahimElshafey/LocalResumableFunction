using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;


internal class ReplayWaitProcessor : IReplayWaitProcessor
{
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitsRepo _waitsRepo;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;

    public ReplayWaitProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitsRepo waitsRepo,
        IWaitTemplatesRepo templatesRepo)
    {
        _logger = logger;
        _waitsRepo = waitsRepo;
        _waitTemplatesRepo = templatesRepo;
    }

    public async Task<(Wait Wait, bool ProceedExecution)> ReplayWait(ReplayRequest replayRequest)
    {
        var oldWaitForReplay = await _waitsRepo.GetOldWaitForReplay(replayRequest);
        if (oldWaitForReplay == null)
        {
            var errorMsg = $"Replay failed because there is no wait to replay, replay request was ({replayRequest})";
            _logger.LogWarning(errorMsg);
            throw new Exception(errorMsg);
        }

        oldWaitForReplay.Status = oldWaitForReplay.Status == WaitStatus.Waiting ? WaitStatus.Canceled : oldWaitForReplay.Status;
        oldWaitForReplay.CurrentFunction = replayRequest.CurrentFunction;

        await _waitsRepo.CancelFunctionWaits(oldWaitForReplay.RequestedByFunctionId, oldWaitForReplay.FunctionStateId);

        switch (replayRequest.ReplayType)
        {
            case ReplayType.GoAfter:
                replayRequest.FunctionState?.AddLog(
                    $"Try to go back after wait `{oldWaitForReplay.Name}`.", LogType.Info, StatusCodes.Replay);
                return new(oldWaitForReplay, true);

            case ReplayType.GoBefore:
                replayRequest.FunctionState?.AddLog(
                    $"Try to go back before wait `{oldWaitForReplay.Name}`.", LogType.Info, StatusCodes.Replay);
                return new(await ReplayGoBefore(oldWaitForReplay), false);

            case ReplayType.GoBeforeWithNewMatch:
                replayRequest.FunctionState?.AddLog(
                    $"Try to go back before wait `{oldWaitForReplay.Name}` with new match.", LogType.Info, StatusCodes.Replay);
                return new(await ReplayGoBeforeWithNewMatch(replayRequest, oldWaitForReplay), false);

            case ReplayType.GoTo:
                replayRequest.FunctionState?.AddLog(
                    $"Try go to wait `{oldWaitForReplay.Name}`.", LogType.Info, StatusCodes.Replay);
                return new(await GetWaitDuplicationAsync(oldWaitForReplay), false);

            case ReplayType.GoToWithNewMatch:
                replayRequest.FunctionState?.AddLog(
                    $"Try go to wait `{oldWaitForReplay.Name}` with new match.", LogType.Info, StatusCodes.Replay);
                return new(await GetWaitDuplicationWithNewMatch(replayRequest, oldWaitForReplay), false);

            default:
                var errorMsg = $"ReplayWait type not defined `{replayRequest}`.";
                _logger.LogWarning(errorMsg);
                oldWaitForReplay.FunctionState?.AddError(errorMsg, StatusCodes.Replay, null);
                throw new Exception(errorMsg);
        }
    }

    private async Task<MethodWait> GetWaitDuplicationWithNewMatch(ReplayRequest replayRequest, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait methodWaitToReplay)
        {
            if (methodWaitToReplay.Template == null)
            {
                methodWaitToReplay.Template = await _waitTemplatesRepo.GetWaitTemplateWithBasicMatch(methodWaitToReplay.TemplateId);
            }
            methodWaitToReplay.LoadExpressions();

            if (ReplayMatchIsSameSignature(replayRequest, methodWaitToReplay) is false)
                return null;

            if (waitToReplay.DuplicateWait() is MethodWait duplicateWait)
            {
                duplicateWait.ActionOnWaitsTree(wait =>
                {
                    wait.Name += $"-Replay-{DateTime.Now.Ticks}";
                    wait.IsReplay = true;
                    wait.IsFirst = false;
                    wait.CurrentFunction = (ResumableFunctionsContainer)duplicateWait.FunctionState.StateObject;
                    wait.Status = WaitStatus.Waiting;
                });


                var template = await AddWaitTemplateIfNotExist(
                    replayRequest.MatchExpression,
                    methodWaitToReplay.SetDataCall,
                    methodWaitToReplay.CancelMethodData,
                    replayRequest.RequestedByFunctionId,
                    methodWaitToReplay.MethodGroupToWaitId,
                    methodWaitToReplay.MethodToWaitId ?? 0,
                    replayRequest.CurrentFunction,
                    methodWaitToReplay.InCodeLine);
                duplicateWait.TemplateId = template.Id;
                await _waitsRepo.SaveWait(duplicateWait);
                return duplicateWait;
            }
        }

        var errorMsg = $"When the replay type is [{ReplayType.GoToWithNewMatch}]" +
                       $"the wait to replay  must be of type [{nameof(MethodWait)}]";
        waitToReplay.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
        throw new Exception(errorMsg);

    }

    private async Task<WaitTemplate> AddWaitTemplateIfNotExist(
        LambdaExpression matchExpression,
        LambdaExpression setDataExpression,
        MethodData cancelMethodData,
        int funcId,
        int groupId,
        int methodId,
        object functionInstance,
        int inCodeLine)
    {
        var waitExpressionsHash = new ExpressionsHashCalculator(matchExpression, setDataExpression, cancelMethodData);
        var expressionsHash = waitExpressionsHash.Hash;
        return 
            await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, groupId) ??
            await _waitTemplatesRepo.AddNewTemplate(waitExpressionsHash, functionInstance, funcId, groupId, methodId, inCodeLine);
    }

    private async Task<Wait> GetWaitDuplicationAsync(Wait oldWaitToReplay)
    {
        if (oldWaitToReplay.WasFirst)
        {
            const string errorMsg =
                "Go to the first wait with same match will create new separate function instance, " +
                "so execution will not be complete.";
            _logger.LogWarning(errorMsg);
            oldWaitToReplay.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
            return null;
        }


        var duplicateWait = oldWaitToReplay.DuplicateWait();
        duplicateWait.ActionOnWaitsTree(wait =>
        {
            wait.Name += $"-Replay-{DateTime.Now.Ticks}";
            wait.IsReplay = true;
            wait.IsFirst = false;
            wait.CurrentFunction = (ResumableFunctionsContainer)duplicateWait.FunctionState.StateObject;
            wait.Status = WaitStatus.Waiting;
        });
        await _waitsRepo.SaveWait(duplicateWait);
        return duplicateWait;
    }

    private async Task<Wait> ReplayGoBefore(Wait oldCompletedWait)
    {
        if (oldCompletedWait.WasFirst)
        {
            const string errorMessage = "Go before the first wait with same match will create new separate function instance.";
            _logger.LogWarning(errorMessage);
            oldCompletedWait.FunctionState.AddError(errorMessage, StatusCodes.Replay, null);
            return null;
        }

        var goBefore = await GoBefore(oldCompletedWait);
        if (goBefore.HasWait)
        {
            var nextWaitAfterReplay = goBefore.Runner.Current;
            nextWaitAfterReplay.CopyCommonIds(oldCompletedWait);
            await _waitsRepo.SaveWait(nextWaitAfterReplay);
            return nextWaitAfterReplay;
        }

        const string errorMsg = "Replay Go Before found no waits!!";
        _logger.LogError(errorMsg);
        oldCompletedWait.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
        throw new Exception(errorMsg);
    }

    private async Task<Wait> ReplayGoBeforeWithNewMatch(ReplayRequest replayWait, Wait oldWaitToReplay)
    {
        if (oldWaitToReplay is MethodWait oldMethodWait)
        {
            var goBefore = await GoBefore(oldWaitToReplay);
            if (goBefore is { HasWait: true, Runner.Current: MethodWait methodWaitToReplay })
            {
                if (ReplayMatchIsSameSignature(replayWait, methodWaitToReplay) is false)
                    return null;


                var template = await AddWaitTemplateIfNotExist(
                     replayWait.MatchExpression,
                     methodWaitToReplay.SetDataCall,
                     methodWaitToReplay.CancelMethodData,
                     oldMethodWait.RequestedByFunctionId,
                     oldMethodWait.MethodGroupToWaitId,
                     oldMethodWait.MethodToWaitId ?? 0,
                     replayWait.CurrentFunction,
                     oldMethodWait.InCodeLine);

                methodWaitToReplay.FunctionState = replayWait.FunctionState;
                methodWaitToReplay.FunctionStateId = replayWait.FunctionStateId;
                methodWaitToReplay.RequestedByFunction = oldWaitToReplay.RequestedByFunction;
                methodWaitToReplay.RequestedByFunctionId = oldWaitToReplay.RequestedByFunctionId;
                methodWaitToReplay.ParentWaitId = oldWaitToReplay.ParentWaitId;
                methodWaitToReplay.TemplateId = template.Id;
                await _waitsRepo.SaveWait(methodWaitToReplay);
                return methodWaitToReplay;
            }

            const string errorMsg = "Replay Go Before with new match found no waits!!";
            _logger.LogError(errorMsg);
            oldWaitToReplay.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
            throw new Exception(errorMsg);
        }

        var message = $"When the replay type is [{ReplayType.GoBeforeWithNewMatch}]" +
                      $"the wait to replay  must be of type [{nameof(MethodWait)}]";
        _logger.LogError(message, null, StatusCodes.Replay);
        oldWaitToReplay.FunctionState.AddError(message, StatusCodes.Replay, null);
        throw new Exception(message);
    }

    private bool ReplayMatchIsSameSignature(ReplayRequest replayWait, MethodWait mw)
    {
        var isSameSignature =
            CoreExtensions.SameMatchSignature(replayWait.MatchExpression, mw.MatchExpression);
        if (isSameSignature is false)
            throw new Exception("Replay match expression method must have same signature as " +
                                "the wait that will replayed.");
        return true;
    }

    private async Task<(FunctionRunner Runner, bool HasWait)> GoBefore(Wait oldCompletedWait)
    {
        var runner = new FunctionRunner(oldCompletedWait.CurrentFunction,
            oldCompletedWait.RequestedByFunction.MethodInfo, oldCompletedWait.StateBeforeWait);
        var hasWait = await runner.MoveNextAsync();
        if (hasWait)
        {
            var waitToReplay = runner.Current;
            waitToReplay.ActionOnWaitsTree(wait =>
            {
                wait.Name += $"-Replay-{DateTime.Now.Ticks}";
                wait.IsReplay = true;
                wait.IsFirst = false;
            });

        }
        return (runner, hasWait);
    }
}
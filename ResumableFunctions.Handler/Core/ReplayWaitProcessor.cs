using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Core;


internal class ReplayWaitProcessor : IReplayWaitProcessor
{
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitsRepo _waitsRepo;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;
    private readonly IServiceRepo _serviceRepo;

    public ReplayWaitProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitsRepo waitsRepo,
        IWaitTemplatesRepo templatesRepo,
        IServiceRepo serviceRepo)
    {
        _logger = logger;
        _waitsRepo = waitsRepo;
        _waitTemplatesRepo = templatesRepo;
        _serviceRepo = serviceRepo;
    }

    public async Task<WaitEntity> ProcessReplayRequest(ReplayRequest replayRequest)
    {
        try
        {
            var waitForReplayDb = await _waitsRepo.GetOldWaitForReplay(replayRequest);
            if (waitForReplayDb == null)
            {
                var errorMsg = $"Replay failed because there is no wait to replay, replay request was ({replayRequest})";
                _logger.LogWarning(errorMsg);
                throw new Exception(errorMsg);
            }

            waitForReplayDb.Status = 
                waitForReplayDb.Status == WaitStatus.Waiting ?
                WaitStatus.Canceled :
                waitForReplayDb.Status;
            waitForReplayDb.CurrentFunction = replayRequest.CurrentFunction;
            waitForReplayDb.InCodeLine = replayRequest.InCodeLine;
            waitForReplayDb.CallerName = replayRequest.CallerName;

            await _waitsRepo.CancelFunctionPendingWaits(waitForReplayDb);

            switch (replayRequest.ReplayType)
            {
                //case ReplayType.GoAfter:
                //    replayRequest.FunctionState?.AddLog(
                //        $"Try to go back after wait [{waitForReplayDb.Name}].", LogType.Info, StatusCodes.Replay);
                //    return waitForReplayDb;

                //case ReplayType.GoBefore:
                //    replayRequest.FunctionState?.AddLog(
                //        $"Try to go back before wait [{waitForReplayDb.Name}].", LogType.Info, StatusCodes.Replay);
                //    return await ReplayGoBefore(waitForReplayDb);

                case ReplayType.GoBeforeWithNewMatch:
                    replayRequest.FunctionState?.AddLog(
                        $"Try to go back before wait [{waitForReplayDb.Name}] with new match.", LogType.Info, StatusCodes.Replay);
                    return await ReplayGoBeforeWithNewMatch(replayRequest, waitForReplayDb);

                //case ReplayType.GoTo:
                //    replayRequest.FunctionState?.AddLog(
                //        $"Try go to wait [{waitForReplayDb.Name}].", LogType.Info, StatusCodes.Replay);
                //    return await GetWaitDuplication(waitForReplayDb);

                case ReplayType.GoToWithNewMatch:
                    replayRequest.FunctionState?.AddLog(
                        $"Try go to wait [{waitForReplayDb.Name}] with new match.", LogType.Info, StatusCodes.Replay);
                    return await GetWaitDuplicationWithNewMatch(replayRequest, waitForReplayDb);

                default:
                    var errorMsg = $"ReplayWait type not defined [{replayRequest}].";
                    _logger.LogWarning(errorMsg);
                    waitForReplayDb.FunctionState?.AddError(errorMsg, StatusCodes.Replay, null);
                    throw new Exception(errorMsg);
            }
        }
        catch (Exception ex)
        {
            await _serviceRepo.AddErrorLog(ex, "Error when replay", StatusCodes.Replay);
            throw;
        }
    }
    private async Task<MethodWaitEntity> GetWaitDuplicationWithNewMatch(ReplayRequest replayRequest, WaitEntity waitToReplayDb)
    {
        if (waitToReplayDb is MethodWaitEntity oldMethodWaitToReplayDb)
        {
            if (oldMethodWaitToReplayDb.Template == null)
            {
                oldMethodWaitToReplayDb.Template = await _waitTemplatesRepo.GetWaitTemplateWithBasicMatch(oldMethodWaitToReplayDb.TemplateId);
            }
            oldMethodWaitToReplayDb.LoadExpressions();

            if (ReplayMatchIsSameSignature(replayRequest, oldMethodWaitToReplayDb) is false)
                return null;

            if (waitToReplayDb.DuplicateWait() is MethodWaitEntity duplicateDbWait)
            {
                duplicateDbWait.Name += $"-Replay-{DateTime.Now.Ticks}";
                duplicateDbWait.IsReplay = true;
                duplicateDbWait.IsFirst = false;
                duplicateDbWait.CurrentFunction = (ResumableFunctionsContainer)duplicateDbWait.FunctionState.StateObject;
                duplicateDbWait.Status = WaitStatus.Waiting;



                var template = await AddWaitTemplateIfNotExist(
                    replayRequest.MatchExpression,
                    oldMethodWaitToReplayDb.AfterMatchAction,
                    oldMethodWaitToReplayDb.CancelMethodAction,
                    replayRequest.RequestedByFunctionId,
                    oldMethodWaitToReplayDb.MethodGroupToWaitId,
                    oldMethodWaitToReplayDb.MethodToWaitId ?? 0,
                    replayRequest.CurrentFunction,
                    oldMethodWaitToReplayDb.InCodeLine,
                    duplicateDbWait);
                duplicateDbWait.TemplateId = template.Id;
                await _waitsRepo.SaveWait(duplicateDbWait);
                duplicateDbWait.RuntimeClosureId = null;
                duplicateDbWait.RuntimeClosure = oldMethodWaitToReplayDb.RuntimeClosure;
                return duplicateDbWait;
            }
        }

        var errorMsg = $"When the replay type is [{ReplayType.GoToWithNewMatch}]" +
                       $"the wait to replay  must be of type [{nameof(MethodWaitEntity)}]";
        waitToReplayDb.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
        throw new Exception(errorMsg);

    }

    private async Task<WaitTemplate> AddWaitTemplateIfNotExist(
        LambdaExpression matchExpression,
        string afterMatchAction,
        string cancelMethodAction,
        int funcId,
        int groupId,
        int methodId,
        object functionInstance,
        int inCodeLine,
        MethodWaitEntity newReplayWait)
    {

        var matchExpressionParts =
            new MatchExpressionWriter(matchExpression, functionInstance).MatchExpressionParts;
        if (matchExpressionParts.Closure != null)
            newReplayWait.SetImmutableClosure(matchExpressionParts.Closure);
        newReplayWait.MandatoryPart = matchExpressionParts.GetInstanceMandatoryPart(functionInstance);

        var expressionsHash =
            new ExpressionsHashCalculator(matchExpressionParts.MatchExpression, afterMatchAction, cancelMethodAction)
            .GetHash();

        return
            await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, groupId) ??
            await _waitTemplatesRepo.AddNewTemplate(
                expressionsHash,
                functionInstance,
                funcId,
                groupId,
                methodId,
                inCodeLine,
                cancelMethodAction,
                afterMatchAction,
                matchExpressionParts);
    }

    private async Task<WaitEntity> GetWaitDuplication(WaitEntity oldWaitToReplay)
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
        duplicateWait.ActionOnChildrenTree(wait =>
        {
            wait.Name += $"-Replay-{DateTime.Now.Ticks}";
            wait.IsReplay = true;
            wait.IsFirst = false;
            wait.CurrentFunction = (ResumableFunctionsContainer)duplicateWait.FunctionState.StateObject;
            wait.Status = WaitStatus.Waiting;
        });
        await _waitsRepo.SaveWait(duplicateWait);
        duplicateWait.RuntimeClosureId = null;
        //todo:closure may be from normall method and continuation may reuse same old private method data??
        duplicateWait.RuntimeClosure = oldWaitToReplay.RuntimeClosure;
        return duplicateWait;
    }

    private async Task<WaitEntity> ReplayGoBefore(WaitEntity oldCompletedWait)
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
            var nextWaitAfterReplay = goBefore.Runner.CurrentWait;
            nextWaitAfterReplay.CopyCommonIds(oldCompletedWait);
            await _waitsRepo.SaveWait(nextWaitAfterReplay);
            nextWaitAfterReplay.RuntimeClosureId = null;
            nextWaitAfterReplay.RuntimeClosure = oldCompletedWait.RuntimeClosure;
            return nextWaitAfterReplay;
        }

        const string errorMsg = "Replay Go Before found no waits!!";
        _logger.LogError(errorMsg);
        oldCompletedWait.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
        throw new Exception(errorMsg);
    }

    private async Task<WaitEntity> ReplayGoBeforeWithNewMatch(ReplayRequest replayWait, WaitEntity waitToReplayDb)
    {
        if (waitToReplayDb is MethodWaitEntity oldMethodWait)
        {
            var goBefore = await GoBefore(waitToReplayDb);
            if (goBefore is { HasWait: true, Runner.CurrentWait: MethodWaitEntity methodWaitToReplayFresh })
            {
                if (ReplayMatchIsSameSignature(replayWait, methodWaitToReplayFresh) is false)
                    return null;


                var template = await AddWaitTemplateIfNotExist(
                     replayWait.MatchExpression,
                     methodWaitToReplayFresh.AfterMatchAction,
                     methodWaitToReplayFresh.CancelMethodAction,
                     oldMethodWait.RequestedByFunctionId,
                     oldMethodWait.MethodGroupToWaitId,
                     oldMethodWait.MethodToWaitId ?? 0,
                     replayWait.CurrentFunction,
                     oldMethodWait.InCodeLine,
                     methodWaitToReplayFresh);

                methodWaitToReplayFresh.FunctionState = replayWait.FunctionState;
                methodWaitToReplayFresh.FunctionStateId = replayWait.FunctionStateId;
                methodWaitToReplayFresh.RequestedByFunction = waitToReplayDb.RequestedByFunction;
                methodWaitToReplayFresh.RequestedByFunctionId = waitToReplayDb.RequestedByFunctionId;
                methodWaitToReplayFresh.ParentWaitId = waitToReplayDb.ParentWaitId;
                methodWaitToReplayFresh.TemplateId = template.Id;
                await _waitsRepo.SaveWait(methodWaitToReplayFresh);
                methodWaitToReplayFresh.RuntimeClosureId = null;
                methodWaitToReplayFresh.RuntimeClosure = waitToReplayDb.RuntimeClosure;
                return methodWaitToReplayFresh;
            }

            const string errorMsg = "Replay Go Before with new match found no waits!!";
            _logger.LogError(errorMsg);
            waitToReplayDb.FunctionState.AddError(errorMsg, StatusCodes.Replay, null);
            throw new Exception(errorMsg);
        }

        var message = $"When the replay type is [{ReplayType.GoBeforeWithNewMatch}]" +
                      $"the wait to replay  must be of type [{nameof(MethodWaitEntity)}]";
        _logger.LogError(message, null, StatusCodes.Replay);
        waitToReplayDb.FunctionState.AddError(message, StatusCodes.Replay, null);
        throw new Exception(message);
    }

    private bool ReplayMatchIsSameSignature(ReplayRequest replayWait, MethodWaitEntity mw)
    {
        var isSameSignature =
            CoreExtensions.SameMatchSignature(replayWait.MatchExpression, mw.MatchExpression);
        if (isSameSignature is false)
            throw new Exception("Replay match expression method must have same signature as " +
                                "the wait that will replayed.");
        return true;
    }

    private async Task<(FunctionRunner Runner, bool HasWait)> GoBefore(WaitEntity oldCompletedWait)
    {
        var runner = new FunctionRunner(
            oldCompletedWait.CurrentFunction,
            oldCompletedWait.RequestedByFunction.MethodInfo,
            oldCompletedWait.StateBeforeWait, 
            oldCompletedWait.RuntimeClosure?.Value);
        var hasWait = await runner.MoveNextAsync();
        if (hasWait)
        {
            var waitToReplay = runner.CurrentWait;
            waitToReplay.ActionOnChildrenTree(wait =>
            {
                wait.Name += $"-Replay-{DateTime.Now.Ticks}";
                wait.IsReplay = true;
                wait.IsFirst = false;
            });

        }
        return (runner, hasWait);
    }
}
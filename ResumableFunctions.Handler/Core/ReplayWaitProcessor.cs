using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using System.Linq.Expressions;
using ResumableFunctions.Handler.Helpers.Expressions;

namespace ResumableFunctions.Handler.Core;


internal class ReplayWaitProcessor : IReplayWaitProcessor
{
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly FunctionDataContext _context;
    private readonly IWaitsRepo _waitsRepo;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;

    public ReplayWaitProcessor(
        FunctionDataContext context,
        ILogger<ReplayWaitProcessor> logger,
        IWaitsRepo waitsRepo,
        IWaitTemplatesRepo templatesRepo)
    {
        _context = context;
        _logger = logger;
        _waitsRepo = waitsRepo;
        _waitTemplatesRepo = templatesRepo;
    }

    public async Task<(Wait Wait, bool ProceedExecution)> ReplayWait(ReplayRequest replayRequest)
    {
        var waitToReplay = await _waitsRepo.GetOldWaitForReplay(replayRequest);
        if (waitToReplay == null)
        {
            string errorMsg = $"Replay failed because there is no wait to replay, replay request was ({replayRequest})";
            _logger.LogWarning(errorMsg);
            throw new Exception(errorMsg);
        }

        //todo:review CancelFunctionWaits is suffecient
        //Cancel wait and it's child
        waitToReplay.Status = waitToReplay.Status == WaitStatus.Waiting ? WaitStatus.Canceled : waitToReplay.Status;
        await _waitsRepo.CancelSubWaits(waitToReplay.Id);
        //skip active waits after replay
        await _waitsRepo.CancelFunctionWaits(waitToReplay.RequestedByFunctionId, waitToReplay.FunctionStateId);

        switch (replayRequest.ReplayType)
        {
            case ReplayType.GoAfter:
                return new(waitToReplay, true);

            case ReplayType.GoBefore:
                return new(await ReplayGoBefore(waitToReplay), false);

            case ReplayType.GoBeforeWithNewMatch:
                return new(await ReplayGoBeforeWithNewMatch(replayRequest, waitToReplay), false);

            case ReplayType.GoTo:
                return new(await GetWaitDuplicationAsync(waitToReplay), false);

            case ReplayType.GoToWithNewMatch:
                return new(await GetWaitDuplicationWithNewMatch(replayRequest, waitToReplay), false);

            default:
                var errorMsg = $"ReplayWait type not defined `{replayRequest}`.";
                _logger.LogWarning(errorMsg);
                waitToReplay.FunctionState.AddError(errorMsg);
                throw new Exception(errorMsg);
        }
    }

    private async Task<MethodWait> GetWaitDuplicationWithNewMatch(ReplayRequest replayRequest, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait mw)
        {
            mw.LoadExpressions();//todo: expression not loaded
            var oldMatchExpression = mw.MatchExpression;


            if (ReplayMatchIsSameSignature(replayRequest, mw) is false)
                return null;

            var duplicateWait = waitToReplay.DuplicateWait() as MethodWait;
            duplicateWait.Name += $"-Replay-{DateTime.Now.Ticks}";
            duplicateWait.IsReplay = true;
            duplicateWait.IsFirst = false;

            ////todo:recalc mandatory part
            //duplicateWait.MandatoryPart = rewriteMatchExpression.MandatoryPart;
            var template = await AddWaitTemplate(
                replayRequest.MatchExpression,
                mw.SetDataExpression,
                mw.RequestedByFunctionId,
                mw.MethodGroupToWaitId,
                mw.MethodToWaitId ?? 0,
                replayRequest.CurrentFunction);
            duplicateWait.TemplateId = template.Id;
            await _waitsRepo.SaveWaitRequestToDb(duplicateWait);
            return duplicateWait;
        }
        else
        {
            string errorMsg = $"When the replay type is [{ReplayType.GoToWithNewMatch}]" +
                              $"the wait to replay  must be of type [{nameof(MethodWait)}]";
            waitToReplay.FunctionState.AddError(errorMsg);
            throw new Exception(errorMsg);
        }

    }

    private async Task<WaitTemplate> AddWaitTemplate(
        LambdaExpression matchExpression,
        LambdaExpression setDataExpression,
        int funcId,
        int groupId,
        int methodId,
        object functionInstance)
    {
        var waitExpressionsHash = new WaitExpressionsHash(matchExpression, setDataExpression);
        var expressionsHash = waitExpressionsHash.Hash;
        var waitTemplate = await _waitTemplatesRepo.CheckTemplateExist(expressionsHash, funcId, groupId);
        if (waitTemplate == null)
            waitTemplate = await _waitTemplatesRepo.AddNewTemplate(
                waitExpressionsHash, functionInstance, funcId, groupId, methodId);
        return waitTemplate;
    }

    private async Task<Wait> GetWaitDuplicationAsync(Wait waitToReplay)
    {
        if (waitToReplay.IsFirst)
        {
            const string errorMsg =
                "Go to the first wait with same match will create new separate function instance, " +
                "so execution will not be complete.";
            _logger.LogWarning(errorMsg);
            waitToReplay.FunctionState.AddError(errorMsg);
            return null;
        }
        var duplicateWait = waitToReplay.DuplicateWait();
        duplicateWait.Name += $"-Replay-{DateTime.Now.Ticks}";
        duplicateWait.IsReplay = true;
        duplicateWait.IsFirst = false;
        await _waitsRepo.SaveWaitRequestToDb(duplicateWait);
        return duplicateWait;
    }

    private async Task<Wait> ReplayGoBefore(Wait oldCompletedWait)
    {
        if (oldCompletedWait.IsFirst)
        {
            const string errorMessage = "Go before the first wait with same match will create new separate function instance.";
            _logger.LogWarning(errorMessage);
            oldCompletedWait.FunctionState.AddError(errorMessage);
            return null;
        }

        //oldCompletedWait.Status = WaitStatus.Canceled;
        var goBefore = await GoBefore(oldCompletedWait);
        if (goBefore.HasWait)
        {
            var nextWaitAfterReplay = goBefore.Runner.Current;
            nextWaitAfterReplay.CopyCommonIds(oldCompletedWait);
            await _waitsRepo.SaveWaitRequestToDb(nextWaitAfterReplay);
            return nextWaitAfterReplay;//when replay go before
        }
        else
        {
            const string errorMsg = "Replay Go Before found no waits!!";
            _logger.LogError(errorMsg);
            oldCompletedWait.FunctionState.AddError(errorMsg);
            throw new Exception(errorMsg);
        }
    }

    private async Task<Wait> ReplayGoBeforeWithNewMatch(ReplayRequest replayWait, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait)
        {
            var goBefore = await GoBefore(waitToReplay);
            if (goBefore is { HasWait: true, Runner.Current: MethodWait mw })
            {
                if (ReplayMatchIsSameSignature(replayWait, mw) is false)
                    return null;

                var template = await AddWaitTemplate(
                     replayWait.MatchExpression,
                     mw.SetDataExpression,
                     mw.RequestedByFunctionId,
                     mw.MethodGroupToWaitId,
                     mw.MethodToWaitId ?? 0,
                     replayWait.CurrentFunction);
                mw.FunctionState = replayWait.FunctionState;
                mw.FunctionStateId = replayWait.FunctionStateId;
                mw.RequestedByFunction = waitToReplay.RequestedByFunction;
                mw.RequestedByFunctionId = waitToReplay.RequestedByFunctionId;
                mw.ParentWaitId = waitToReplay.ParentWaitId;
                mw.TemplateId = template.Id;
                await _waitsRepo.SaveWaitRequestToDb(mw);
                return mw;
            }
            else
            {
                const string errorMsg = "Replay Go Before with ne match found no waits!!";
                _logger.LogError(errorMsg);
                waitToReplay.FunctionState.AddError(errorMsg);
                throw new Exception(errorMsg);
            }
        }
        else
        {
            string message = $"When the replay type is [{ReplayType.GoBeforeWithNewMatch}]" +
                            $"the wait to replay  must be of type [{nameof(MethodWait)}]";
            _logger.LogError(message);
            waitToReplay.FunctionState.AddError(message);
            throw new Exception(message);
        }
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
            waitToReplay.Name += "-Replay";
            waitToReplay.IsReplay = true;
            waitToReplay.IsFirst = false;
        }
        return (runner, hasWait);
    }
}
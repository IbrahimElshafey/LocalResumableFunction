using System.Diagnostics;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.DataAccess;

namespace ResumableFunctions.Handler.Core;


internal class ReplayWaitProcessor : IReplayWaitProcessor
{
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly FunctionDataContext _context;
    private readonly IWaitsRepository _waitsRepository;

    public ReplayWaitProcessor(
        FunctionDataContext context,
        ILogger<ReplayWaitProcessor> logger,
        IWaitsRepository waitsRepository)
    {
        _context = context;
        _logger = logger;
        _waitsRepository = waitsRepository;
    }

    public async Task<(Wait Wait, bool ProceedExecution)> ReplayWait(ReplayRequest replayRequest)
    {
        var waitToReplay = await _waitsRepository.GetOldWaitForReplay(replayRequest);
        if (waitToReplay == null)
        {
            string errorMsg = $"Replay failed because there is no wait to replay, replay request was ({replayRequest})";
            _logger.LogWarning(errorMsg);
            throw new Exception(errorMsg);
        }

        //todo:review CancelFunctionWaits is suffecient
        //Cancel wait and it's child
        waitToReplay.Status = waitToReplay.Status == WaitStatus.Waiting ? WaitStatus.Canceled : waitToReplay.Status;
        await _waitsRepository.CancelSubWaits(waitToReplay.Id);
        //skip active waits after replay
        await _waitsRepository.CancelFunctionWaits(waitToReplay.RequestedByFunctionId, waitToReplay.FunctionStateId);

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
            mw.LoadExpressions();
            var oldMatchExpression = mw.MatchIfExpression;
            mw.MatchIfExpression = replayRequest.MatchExpression;
            replayRequest.MatchExpression = new RewriteMatchExpression(mw).MatchExpression;
            mw.MatchIfExpression = oldMatchExpression;
            CheckReplayMatchExpression(replayRequest, mw);

            var duplicateWait = waitToReplay.DuplicateWait() as MethodWait;
            duplicateWait.Name += $"-Replay-{DateTime.Now.Ticks}";
            duplicateWait.IsReplay = true;
            duplicateWait.IsFirst = false;
            duplicateWait.MatchIfExpression = replayRequest.MatchExpression;
            await _waitsRepository.SaveWaitRequestToDb(duplicateWait);
            return duplicateWait;// when replay goto with new match
        }
        else
        {
            string errorMsg = $"When the replay type is [{ReplayType.GoToWithNewMatch}]" +
                              $"the wait to replay  must be of type [{nameof(MethodWait)}]";
            waitToReplay.FunctionState.AddError(errorMsg);
            throw new Exception(errorMsg);
        }

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
        await _waitsRepository.SaveWaitRequestToDb(duplicateWait);
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
            nextWaitAfterReplay.CopyFromOld(oldCompletedWait);
            await _waitsRepository.SaveWaitRequestToDb(nextWaitAfterReplay);
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
                CheckReplayMatchExpression(replayWait, mw);

                mw.MatchIfExpression = replayWait.MatchExpression;
                mw.FunctionState = replayWait.FunctionState;
                mw.FunctionStateId = replayWait.FunctionStateId;
                mw.RequestedByFunction = waitToReplay.RequestedByFunction;
                mw.RequestedByFunctionId = waitToReplay.RequestedByFunctionId;
                mw.ParentWaitId = waitToReplay.ParentWaitId;
                await _waitsRepository.SaveWaitRequestToDb(mw);
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

    private void CheckReplayMatchExpression(ReplayRequest replayWait, MethodWait mw)
    {
        var isSameSignature =
            CoreExtensions.SameMatchSignature(replayWait.MatchExpression, mw.MatchIfExpression);
        if (isSameSignature is false)
            throw new Exception("Replay match expression method must have same signature as " +
                                "the wait that will replayed.");
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
using System.Diagnostics;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ResumableFunctions.Handler;

public partial class ResumableFunctionHandler
{
    private async Task ReplayWait(ReplayRequest replayRequest)
    {
        var waitToReplay = await _context.waitsRepository.GetOldWaitForReplay(replayRequest);
        if (waitToReplay == null)
        {
            _logger.LogWarning($"Replay failed, replay is ({replayRequest})");
            return;
        }

        //todo:review CancelFunctionWaits is suffecient
        //Cancel wait and it's child
        waitToReplay.Status = waitToReplay.Status == WaitStatus.Waiting ? WaitStatus.Canceled : waitToReplay.Status;
        await _context.waitsRepository.CancelSubWaits(waitToReplay.Id);
        //skip active waits after replay
        await _context.waitsRepository.CancelFunctionWaits(waitToReplay.RequestedByFunctionId, waitToReplay.FunctionStateId);

        switch (replayRequest.ReplayType)
        {
            case ReplayType.GoAfter:
                await ProceedToNextWait(waitToReplay);
                break;
            case ReplayType.GoBefore:
                await ReplayGoBefore(waitToReplay);
                break;
            case ReplayType.GoBeforeWithNewMatch:
                await ReplayGoBeforeWithNewMatch(replayRequest, waitToReplay);
                break;
            case ReplayType.GoTo:
                await ReplayGoTo(waitToReplay);
                break;
            case ReplayType.GoToWithNewMatch:
                await ReplayGoToWithNewMatch(replayRequest, waitToReplay);
                break;
            default:
                _logger.LogWarning("ReplayWait type not defined.");
                break;
        }
    }

    private async Task ReplayGoToWithNewMatch(ReplayRequest replayRequest, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait mw)
        {
            mw.LoadExpressions();
            var oldMatchExpression = mw.MatchIfExpression;
            mw.MatchIfExpression = replayRequest.MatchExpression;
            replayRequest.MatchExpression = new RewriteMatchExpression(mw).Result;
            mw.MatchIfExpression = oldMatchExpression;
            CheckReplayMatchExpression(replayRequest, mw);

            var duplicateWait = waitToReplay.DuplicateWait() as MethodWait;
            duplicateWait.Name += $"-Replay-{DateTime.Now.Ticks}";
            duplicateWait.IsReplay = true;
            duplicateWait.IsFirst = false;
            duplicateWait.MatchIfExpression = replayRequest.MatchExpression;
            await SaveWaitRequestToDb(duplicateWait);// when replay goto with new match
        }
        else
        {
            throw new Exception($"When the replay type is [{ReplayType.GoToWithNewMatch}]" +
                                $"the wait to replay  must be of type [{nameof(MethodWait)}]");
        }

    }

    private async Task ReplayGoTo(Wait waitToReplay)
    {
        if (waitToReplay.IsFirst)
        {
            WriteMessage("Go to the first wait with same match will create new separate function instance.");
            return;
        }
        var duplicateWait = waitToReplay.DuplicateWait();
        duplicateWait.Name += $"-Replay-{DateTime.Now.Ticks}";
        duplicateWait.IsReplay = true;
        duplicateWait.IsFirst = false;
        await SaveWaitRequestToDb(duplicateWait);//when replay go to
    }

    private async Task ReplayGoBefore(Wait oldCompletedWait)
    {
        if (oldCompletedWait.IsFirst)
        {
            WriteMessage("Go before the first wait with same match will create new separate function instance.");
            return;
        }

        //oldCompletedWait.Status = WaitStatus.Canceled;
        var goBefore = await GoBefore(oldCompletedWait);
        if (goBefore.HasWait)
        {
            var nextWaitAfterReplay = goBefore.Runner.Current;
            nextWaitAfterReplay.CopyFromOld(oldCompletedWait);
            await SaveWaitRequestToDb(nextWaitAfterReplay);//when replay go before
        }
        else
        {
            _logger.LogWarning("Replay Go Before found no waits!!");
        }
    }

    private async Task ReplayGoBeforeWithNewMatch(ReplayRequest replayWait, Wait waitToReplay)
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
                await SaveWaitRequestToDb(mw);//when go before with new match
            }
        }
        else
        {
            throw new Exception($"When the replay type is [{ReplayType.GoBeforeWithNewMatch}]" +
                                $"the wait to replay  must be of type [{nameof(MethodWait)}]");
        }
    }

    private static void CheckReplayMatchExpression(ReplayRequest replayWait, MethodWait mw)
    {
        var isSameSignature =
            CoreExtensions.SameMatchSignature(replayWait.MatchExpression, mw.MatchIfExpression);
        if (isSameSignature is false)
            throw new Exception("Replay match expression method must have same signature as " +
                                "the wait that will replayed.");
    }

    private static async Task<(FunctionRunner Runner, bool HasWait)> GoBefore(Wait oldCompletedWait)
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

    internal async Task<bool> IsExternal(string methodUrn)
    {
        SetDependencies(_serviceProvider);
        return await _context
            .MethodsGroups
            .Include(x => x.WaitMethodIdentifiers)
            .Where(x => x.MethodGroupUrn == methodUrn)
            .SelectMany(x => x.WaitMethodIdentifiers)
            .AnyAsync(x => x.CanPublishFromExternal);
    }
}
using System.Diagnostics;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task ReplayWait(ReplayWait replayWait)
    {
        var waitToReplay = await _waitsRepository.GetOldWaitForReplay(replayWait);
        if (waitToReplay == null)
        {
            WriteMessage($"Replay failed, replay is ({replayWait})");
            return;
        }
        //Cancel wait and it's child
        waitToReplay.Status = waitToReplay.Status == WaitStatus.Waiting ? WaitStatus.Canceled : waitToReplay.Status;
        await _waitsRepository.CancelSubWaits(waitToReplay.Id);
        //skip active waits after replay
        await _waitsRepository.CancelFunctionWaits(waitToReplay.RequestedByFunctionId, waitToReplay.FunctionStateId);

        //waitToReplay.RequestedByFunction = replayWait.RequestedByFunction;
        //waitToReplay.RequestedByFunctionId = replayWait.RequestedByFunctionId;
        switch (replayWait.ReplayType)
        {
            case ReplayType.GoAfter:
                await ProceedToNextWait(waitToReplay);
                break;
            case ReplayType.GoBefore:
                await ReplayGoBefore(waitToReplay);
                break;
            case ReplayType.GoBeforeWithNewMatch:
                await ReplayGoBeforeWithNewMatch(replayWait, waitToReplay);
                break;
            case ReplayType.GoTo:
                var duplicateWait = waitToReplay.DuplicateWait();
                duplicateWait.Name += "-Replay";
                duplicateWait.IsReplay = true;
                duplicateWait.IsFirst = false;
                await GenericWaitRequested(duplicateWait);
                break;
            default:
                WriteMessage("ReplayWait type not defined.");
                break;
        }
    }


    private async Task ReplayGoBefore(Wait oldCompletedWait)
    {
        if (oldCompletedWait.IsFirst)
        {
            WriteMessage("Go back to first wait with same match will create new separate function instance.");
            return;
        }

        //oldCompletedWait.Status = WaitStatus.Canceled;
        var goBefore = await GoBefore(oldCompletedWait);
        if (goBefore.HasWait)
        {
            var nextWaitAfterReplay = goBefore.Runner.Current;
            nextWaitAfterReplay.CopyFromOld(oldCompletedWait);
            await GenericWaitRequested(nextWaitAfterReplay);
        }
        else
        {
            WriteMessage("Replay Go Before found no waits!!");
        }
    }

    private async Task ReplayGoBeforeWithNewMatch(ReplayWait replayWait, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait)
        {
            var goBefore = await GoBefore(waitToReplay);
            if (goBefore is { HasWait: true, Runner.Current: MethodWait mw })
            {
                var isSameSignature =
                    Extensions.SameLambadaSignatures(replayWait.MatchExpression, mw.MatchIfExpression);
                if (isSameSignature is false)
                    throw new Exception("Replay match expression method must have same signature as " +
                                        "the wait that will replayed.");


                mw.MatchIfExpression = replayWait.MatchExpression;
                mw.FunctionState = replayWait.FunctionState;
                mw.FunctionStateId = replayWait.FunctionStateId;
                mw.RequestedByFunction = waitToReplay.RequestedByFunction;
                mw.RequestedByFunctionId = waitToReplay.RequestedByFunctionId;
                mw.ParentWaitId = waitToReplay.ParentWaitId;
                await GenericWaitRequested(mw);
            }
        }
        else
        {
            throw new Exception($"When the replay type is [{ReplayType.GoBeforeWithNewMatch}]" +
                                $"the wait to replay  must be of type [{nameof(MethodWait)}]");
        }
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


}
using System.Diagnostics;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler
{
    private async Task ReplayWait(ReplayRequest replayRequest)
    {
        var waitToReplay = await _waitsRepository.GetOldWaitForReplay(replayRequest);
        if (waitToReplay == null)
        {
            WriteMessage($"Replay failed, replay is ({replayRequest})");
            return;
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
                await ReplayGoToWithNewMatch(replayRequest,waitToReplay);
                break;
            default:
                WriteMessage("ReplayWait type not defined.");
                break;
        }
    }

    private async Task ReplayGoToWithNewMatch(ReplayRequest replayRequest, Wait waitToReplay)
    {
        if (waitToReplay is MethodWait methodWait)
        {
            CheckReplayMatchExpression(replayRequest, methodWait);

            var duplicateWait = waitToReplay.DuplicateWait() as MethodWait;
            duplicateWait.Name += "-Replay";
            duplicateWait.IsReplay = true;
            duplicateWait.IsFirst = false;
            duplicateWait.MatchIfExpression = replayRequest.MatchExpression;
            await GenericWaitRequested(duplicateWait);
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
        duplicateWait.Name += "-Replay";
        duplicateWait.IsReplay = true;
        duplicateWait.IsFirst = false;
        await GenericWaitRequested(duplicateWait);
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
            await GenericWaitRequested(nextWaitAfterReplay);
        }
        else
        {
            WriteMessage("Replay Go Before found no waits!!");
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
                await GenericWaitRequested(mw);
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
        mw.LoadExpressions();
        var isSameSignature =
            CoreExtensions.SameLambadaSignatures(replayWait.MatchExpression, mw.MatchIfExpression);
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


}
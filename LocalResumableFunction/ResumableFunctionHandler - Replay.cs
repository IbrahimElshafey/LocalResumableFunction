using System.Diagnostics;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task<bool> ReplayWait(ReplayWait replayWait)
    {
        Debugger.Launch();
        var oldWait = await GetOldWaitForReplay(replayWait);
        if (oldWait.IsFail)
        {
            WriteMessage($"Replay failed, replay is ({replayWait})");
            return false;
        }

        var waitToReplay = oldWait.WaitToReplay!;
        waitToReplay.FunctionState = replayWait.FunctionState;
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
            default:
                WriteMessage("ReplayWait type not defined.");
                break;
        }

        return true;
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
            waitToReplay.IsFirst = false;
        }

        return (runner, hasWait);
    }

    private async Task<(bool IsFail, Wait WaitToReplay)> GetOldWaitForReplay(ReplayWait replayWait)
    {
        var functionOldWaits =
            await _waitsRepository.GetFunctionInstanceWaits(replayWait.RequestedByFunctionId,
                replayWait.FunctionState.Id);
        var waitToReplay = functionOldWaits
            .FirstOrDefault(x => x.Name == replayWait.Name && x.IsNode);

        if (waitToReplay == null)
        {
            WriteMessage(
                $"Can't replay not exiting wait [{replayWait.Name}] in function [{replayWait?.RequestedByFunction?.MethodName}].");
            return (true, null);
        }

        waitToReplay.ChildWaits = await _context.Waits.Where(x => x.ParentWaitId == waitToReplay.Id).ToListAsync();
        waitToReplay.Cancel();
        //skip active waits after replay
        //todo:[Critical] this may cause a problem 
        //wait may be a group and code will cancel children
        functionOldWaits
            .Where(x => x.Id > waitToReplay.Id && x.Status == WaitStatus.Waiting)
            .ToList()
            .ForEach(x => x.Status = WaitStatus.Canceled);
        await _context.SaveChangesAsync();
        return (false, waitToReplay);
    }
}
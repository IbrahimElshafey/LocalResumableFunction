using System.Diagnostics;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task<bool> ReplayWait(ReplayWait replayWait)
    {
        Debugger.Launch();
        var oldWait = await GetOldWait(replayWait);
        if (oldWait.IsFail)
        {
            WriteMessage($"Replay failed, replay is ({replayWait})");
            return false;
        }

        var waitToReplay = oldWait.WaitToReplay!;
        switch (replayWait.ReplayType)
        {
            case ReplayType.GoAfter:
                await ReplayGoAfter(waitToReplay);
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

    private async Task ReplayGoAfter(Wait oldCompletedWait)
    {
        var nextWaitResult = await oldCompletedWait.CurrntFunction.GetNextWait(oldCompletedWait);
        await HandleNextWait(nextWaitResult, oldCompletedWait);
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
            await GenericWaitRequested(goBefore.Runner.Current);
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
        var runner = new FunctionRunner(oldCompletedWait.CurrntFunction,
            oldCompletedWait.RequestedByFunction.MethodInfo, oldCompletedWait.StateBeforeWait);
        var hasWait = await runner.MoveNextAsync();
        if (hasWait)
        {
            runner.Current.Name += "-Replay";
            runner.Current.IsFirst = false;
        }

        return (runner, hasWait);
    }

    private async Task<(bool IsFail, Wait WaitToReplay)> GetOldWait(ReplayWait replayWait)
    {
        var functionOldWaits =
            await _waitsRepository.GetFunctionInstanceWaits(replayWait.RequestedByFunctionId,
                replayWait.FunctionState.Id);
        var waitToReplay = functionOldWaits
            .FirstOrDefault(x => x.Status == WaitStatus.Completed && x.Name == replayWait.Name);
        if (waitToReplay == null)
        {
            WriteMessage(
                $"Can't replay not exiting wait [{replayWait.Name}] in function [{replayWait.RequestedByFunction.MethodName}].");
            return (true, null);
        }

        //skip active waits after replay
        functionOldWaits
            .Where(x => x.Id > waitToReplay.Id && x.Status == WaitStatus.Waiting)
            .ToList()
            .ForEach(x => x.Status = WaitStatus.Canceled);
        return (false, waitToReplay);
    }
}
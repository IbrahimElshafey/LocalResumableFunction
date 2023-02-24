using System.Diagnostics;
using System.Xml.Linq;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task HandleMatchedWait(Wait matchedWait)
    {
        var currentWait = matchedWait;

        do
        {
            var parent = await _waitsRepository.GetWaitParent(currentWait);
            switch (currentWait)
            {
                case MethodWait methodWait:
                    methodWait.Status = WaitStatus.Completed;
                    await GoNext(parent, methodWait);
                    break;

                case WaitsGroup:
                case FunctionWait:
                    if (currentWait.IsFinished())
                    {
                        WriteMessage($"Exit ({currentWait.Name})");
                        currentWait.Status = WaitStatus.Completed;
                        await _waitsRepository.CancelSubWaits(currentWait.Id);
                        await GoNext(parent, currentWait);
                    }
                    else return;
                    break;
            }

            var previousChild = currentWait;
            currentWait = parent;
            if (currentWait != null)
                currentWait.FunctionState = previousChild.FunctionState;
        } while (currentWait != null);
    }

    private async Task GoNext(Wait parent, Wait currentWait)
    {
        switch (parent)
        {
            case null:
            case FunctionWait:
                await ProceedToNextWait(currentWait);
                break;
            case WaitsGroup:
                WriteMessage($"Wait group ({parent.Name}) to complete.");
                break;
        }
    }

    private async Task ProceedToNextWait(Wait currentWait)
    {
        if (currentWait.ParentWait != null && currentWait.ParentWait.Status != WaitStatus.Waiting)
        {
            WriteMessage("Can't proceed,Parent wait status is not (Waiting).");
            return;
        }
        currentWait.Status = WaitStatus.Completed;
        var nextWait = await currentWait.GetNextWait();
        if (nextWait == null)
        {
            if (currentWait.ParentWaitId == null)
                await FinalExit(currentWait);
            return;
        }

        WriteMessage($"Get next wait [{nextWait.Name}] after [{currentWait.Name}]");

        nextWait.ParentWaitId = currentWait.ParentWaitId;
        nextWait.FunctionState = currentWait.FunctionState;
        nextWait.RequestedByFunctionId = currentWait.RequestedByFunctionId;
        if (nextWait is ReplayWait replayWait)
            await ReplayWait(replayWait);
        await GenericWaitRequested(nextWait);
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        await _context.SaveChangesAsync();
        await DuplicateIfFirst(currentWait);
    }

    private async Task FinalExit(Wait currentWait)
    {
        WriteMessage("Final Exit");
        currentWait.Status = WaitStatus.Completed;
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        currentWait.FunctionState.IsCompleted = true;
        await _waitsRepository.CancelOpenedWaitsForState(currentWait.FunctionStateId);
        await MoveFunctionToRecycleBin(currentWait);
    }
}
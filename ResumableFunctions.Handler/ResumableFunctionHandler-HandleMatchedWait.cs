using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.InOuts;
using static Azure.Core.HttpHeader;

namespace ResumableFunctions.Handler;

public partial class ResumableFunctionHandler
{
    private async Task ResumeExecution(Wait matchedWait)
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
                    await _context.SaveChangesAsync();
                    break;

                case WaitsGroup:
                case FunctionWait:
                    if (currentWait.IsCompleted())
                    {
                        WriteMessage($"Exit ({currentWait.Name})");
                        currentWait.Status = WaitStatus.Completed;
                        await _waitsRepository.CancelSubWaits(currentWait.Id);
                        await GoNext(parent, currentWait);
                    }
                    else return;
                    break;
            }

            currentWait = parent;

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
        //bug:may cause problem for go back after
        if (currentWait.ParentWait != null && currentWait.ParentWait.Status != WaitStatus.Waiting)
        {
            _logger.LogWarning($"Can't proceed to netx ,Parent wait [{currentWait.ParentWait.Name}] status is not (Waiting).");
            return;
        }
        currentWait.Status = WaitStatus.Completed;
        var nextWait = await currentWait.GetNextWait();
        if (nextWait == null)
        {
            if (currentWait.ParentWaitId == null)
                await FinalExit(currentWait);
            await DuplicateIfFirst(currentWait);
            return;
        }

        WriteMessage($"Get next wait [{nextWait.Name}] after [{currentWait.Name}]");

        nextWait.ParentWaitId = currentWait.ParentWaitId;
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        nextWait.FunctionState = currentWait.FunctionState;
        _context.Entry(nextWait.FunctionState).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        nextWait.RequestedByFunctionId = currentWait.RequestedByFunctionId;

        await SaveWaitRequestToDb(nextWait);//main use
        await _context.SaveChangesAsync();
        await DuplicateIfFirst(currentWait);
    }

    private async Task FinalExit(Wait currentWait)
    {
        WriteMessage("Final Exit");
        currentWait.Status = WaitStatus.Completed;
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        currentWait.FunctionState.LogStatus(FunctionStatus.Completed,"Function instance completed.");
        await _waitsRepository.CancelOpenedWaitsForState(currentWait.FunctionStateId);
        await MoveFunctionToRecycleBin(currentWait);
    }
}
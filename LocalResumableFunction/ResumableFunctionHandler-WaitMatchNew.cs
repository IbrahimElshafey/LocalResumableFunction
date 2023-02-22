using System.Diagnostics;
using System.Xml.Linq;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task HandleMatchedWait(Wait matchedWait)
    {
        Wait previousChild = null;
        Wait currentWait = matchedWait;

        do
        {
            var parent = await _waitsRepository.GetWaitParent(currentWait);
            switch (currentWait)
            {
                case MethodWait methodWait:
                    methodWait.Status = WaitStatus.Completed;
                    switch (parent)
                    {
                        case null:
                        case FunctionWait:
                            await ProceedToNextWait(methodWait);
                            break;
                        case ManyMethodsWait:
                            WriteMessage($"Wait method group ({parent.Name}) to complete.");
                            break;
                    }
                    break;

                case ManyMethodsWait { WaitType: WaitType.AllMethodsWait } allMethodsWait:
                    allMethodsWait.MoveToMatched(previousChild);
                    if (allMethodsWait.IsCompleted)
                    {
                        await ProceedToNextWait(allMethodsWait);
                    }
                    else return;//no backtrace
                    break;

                case ManyMethodsWait { WaitType: WaitType.AnyMethodWait } anyMethodWait:
                    anyMethodWait.SetMatchedMethod(previousChild);
                    if (anyMethodWait.IsCompleted)
                    {
                        await ProceedToNextWait(anyMethodWait);
                    }
                    else return;//no backtrace
                    break;

                case FunctionWait functionWait:
                    var functionCompleted = functionWait.ChildWaits.All(x => x.IsCompleted);
                    if (functionCompleted)
                    {
                        WriteMessage($"Exist function ({functionWait.Name})");
                        functionWait.Status = WaitStatus.Completed;
                        switch (parent)
                        {
                            case null:
                            case FunctionWait:
                                await ProceedToNextWait(functionWait);
                                break;
                            case ManyFunctionsWait:
                                WriteMessage($"Wait function group ({parent.Name}) to complete.");
                                break;

                        }
                    }
                    else return;//no backtrace
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AllFunctionsWait } allFunctionsWait:
                    if (previousChild.IsCompleted)
                    {
                        allFunctionsWait.Status =
                            allFunctionsWait.WaitingFunctions.Count == allFunctionsWait.CompletedFunctions.Count
                                ? WaitStatus.Completed
                                : allFunctionsWait.Status;
                        if (allFunctionsWait.IsCompleted)
                        {
                            WriteMessage($"Exist many functions wait ({allFunctionsWait.Name})");
                            await ProceedToNextWait(allFunctionsWait);
                        }
                    }
                    else return;//no backtrace
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AnyFunctionWait } anyFunctionWait:
                    if (previousChild.IsCompleted)
                    {
                        anyFunctionWait.Status = WaitStatus.Completed;
                        await _waitsRepository.CancelFunctionGroupWaits(anyFunctionWait);
                        if (anyFunctionWait.IsCompleted)
                        {
                            WriteMessage($"Exist many functions wait ({anyFunctionWait.Name})");
                            await ProceedToNextWait(anyFunctionWait);
                        }
                    }
                    else return;//no backtrace
                    break;
            }

            previousChild = currentWait;
            currentWait = parent;
            if (currentWait != null)
                currentWait.FunctionState = previousChild.FunctionState;
        } while (currentWait != null);
    }

    private async Task ProceedToNextWait(Wait currentWait)
    {
        var functionRunner = new FunctionRunner(currentWait);
        if (functionRunner.ResumableFunctionExist is false)
        {
            Debug.WriteLine($"Resumable function ({currentWait.RequestedByFunction.MethodName}) not exist in code");
            //todo:delete it and all related waits
            //throw new Exception("Can't initiate runner");
            return;
        }
        var waitExist = await functionRunner.MoveNextAsync();
        if (!waitExist) return;

        Console.WriteLine($"Get next wait [{functionRunner.Current.Name}] after [{currentWait.Name}]");
        var nextWait = functionRunner.Current;
        nextWait.ParentWaitId = currentWait.ParentWaitId;
        nextWait.FunctionState = currentWait.FunctionState;
        nextWait.RequestedByFunctionId = currentWait.RequestedByFunctionId;
        if (nextWait is ReplayWait replayWait)
            await ReplayWait(replayWait);
        currentWait.Status = WaitStatus.Completed;
        await GenericWaitRequested(nextWait);
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        await _context.SaveChangesAsync();
        await DuplicateIfFirst(currentWait);

        var isFinalEnd = nextWait == null && currentWait.ParentWaitId == null;
        if (isFinalEnd)
            await FinalExit(currentWait);
    }

    private async Task FinalExit(Wait currentWait)
    {
        WriteMessage("Final Exit");
        currentWait.Status = WaitStatus.Completed;
        currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
        currentWait.FunctionState.IsCompleted = true;
        await MoveFunctionToRecycleBin(currentWait);
    }
}
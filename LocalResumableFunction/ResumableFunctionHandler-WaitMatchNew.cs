using System.Diagnostics;
using System.Xml.Linq;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private async Task HandleMatchedWaitNew(Wait matchedWait)
    {
        Wait previousChild = null;
        Wait currentWait = matchedWait;

        do
        {
            switch (currentWait)
            {
                case MethodWait methodWait:
                    methodWait.Status = WaitStatus.Completed;
                    await ProceedToNextWait(methodWait);
                    break;

                case ManyMethodsWait { WaitType: WaitType.AllMethodsWait } allMethodsWait:
                    allMethodsWait.MoveToMatched(previousChild);
                    if (allMethodsWait.IsCompleted)
                    {
                        await ProceedToNextWait(allMethodsWait);
                    }
                    break;

                case ManyMethodsWait { WaitType: WaitType.AnyMethodWait } anyMethodWait:
                    anyMethodWait.SetMatchedMethod(previousChild);
                    if (anyMethodWait.IsCompleted)
                    {
                        await ProceedToNextWait(anyMethodWait);
                    }
                    break;

                case FunctionWait functionWait:
                    var next = await ProceedToNextWait(previousChild);
                    if (next == null)
                    {
                        functionWait.Status = WaitStatus.Completed;
                        WriteMessage($"Exist function {functionWait.Name}");
                    }
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AllFunctionsWait } allFunctionsWait:
                    if (previousChild.IsCompleted)
                    {
                        allFunctionsWait.Status=
                            allFunctionsWait.WaitingFunctions.Count == allFunctionsWait.CompletedFunctions.Count ? WaitStatus.Completed : allFunctionsWait.Status;
                    }
                    if (allFunctionsWait.IsCompleted)
                    {
                        await ProceedToNextWait(allFunctionsWait);
                        WriteMessage($"Exist many functions wait {allFunctionsWait.Name}");
                    }
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AnyFunctionWait } anyFunctionWait:
                    if (previousChild.IsCompleted)
                    {
                        anyFunctionWait.Status = WaitStatus.Completed;
                        await _waitsRepository.CancelFunctionGroupWaits(anyFunctionWait);
                    }
                    if (anyFunctionWait.IsCompleted)
                    {
                        await ProceedToNextWait(anyFunctionWait);
                    }
                    break;
            }

            var parent = await _waitsRepository.GetWaitParent(currentWait);
            previousChild = currentWait;
            currentWait = parent;
            if(currentWait!=null)
                currentWait.FunctionState = previousChild.FunctionState;
        } while (currentWait != null);
    }

    private async Task<Wait> ProceedToNextWait(Wait currentWait)
    {
        var functionRunner = new FunctionRunner(currentWait);
        if (functionRunner.ResumableFunctionExist is false)
        {
            Debug.WriteLine($"Resumable function ({currentWait.RequestedByFunction.MethodName}) not exist in code");
            //todo:delete it and all related waits
            //throw new Exception("Can't initiate runner");
            return null;
        }
        var waitExist = await functionRunner.MoveNextAsync();
        if (!waitExist) return null;

        Console.WriteLine($"Get next wait [{functionRunner.Current.Name}] after [{currentWait.Name}]");
        var nextWait = functionRunner.Current;
        nextWait.ParentWaitId = currentWait.ParentWaitId;
        nextWait.FunctionState = currentWait.FunctionState;
        nextWait.RequestedByFunctionId = currentWait.RequestedByFunctionId;
        if (nextWait is ReplayWait replayWait)
            await ReplayWait(replayWait);
        currentWait.Status = WaitStatus.Completed;
        await GenericWaitRequested(nextWait);
        currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
        await _context.SaveChangesAsync();
        await DuplicateIfFirst(currentWait);

        var isFinalEnd = nextWait == null && currentWait.ParentWaitId == null;
        if (isFinalEnd)
            await FinalExit(currentWait);
        return nextWait;
    }

    private async Task FinalExit(Wait currentWait)
    {
        WriteMessage("Final Exit");
        currentWait.Status = WaitStatus.Completed;
        currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
        currentWait.FunctionState.IsCompleted = true;
        await MoveFunctionToRecycleBin(currentWait);
    }

    //private async Task SubFunctionEnded(Wait functionWait)
    //{
    //    if (functionWait.IsCompleted)
    //    {
    //        WriteMessage("Can't proceed with completed function.");
    //        return;
    //    }

    //    WriteMessage($"Sub Function Ended {functionWait.Name}");
    //    var functionParentWait = await _waitsRepository.GetWaitParent(functionWait);
    //    if (functionParentWait == null)
    //    {
    //        WriteMessage(
    //            $"Root function wait not exist for wait ({functionWait.Name}) with type ({functionWait.WaitType}).");
    //        return;
    //    }

    //    if (functionParentWait.Status != WaitStatus.Waiting)
    //    {
    //        WriteMessage(
    //            $"The status for parent function wait ({functionParentWait.Name}) must be ({WaitStatus.Waiting}).");
    //        return;
    //    }

    //    var backToCaller = false;
    //    functionWait.Status = WaitStatus.Completed;
    //    Debugger.Launch();
    //    switch (functionParentWait)
    //    {
    //        //one sub function -> return to caller after function end
    //        case FunctionWait:
    //            backToCaller = true;
    //            break;
    //        //many sub functions -> wait function group to complete and return to caller
    //        case ManyFunctionsWait allFunctionsWait
    //            when functionParentWait.WaitType == WaitType.AllFunctionsWait:
    //            allFunctionsWait.MoveToMatched(functionWait.Id);
    //            if (allFunctionsWait.Status == WaitStatus.Completed)
    //                backToCaller = true;
    //            break;
    //        case ManyFunctionsWait anyFunctionWait
    //            when functionParentWait.WaitType == WaitType.AnyFunctionWait:
    //            anyFunctionWait.SetMatchedFunction(functionWait.Id);
    //            await _waitsRepository.CancelFunctionGroupWaits(anyFunctionWait);
    //            if (anyFunctionWait.Status == WaitStatus.Completed)
    //                backToCaller = true;
    //            break;
    //        default:
    //            WriteMessage(
    //                $"Root function wait ({functionParentWait.Name}) with type ({functionParentWait.WaitType}) is not valid function wait.");
    //            break;
    //    }

    //    if (backToCaller)
    //    {
    //        //functionParentWait.Status = WaitStatus.Completed;
    //        functionParentWait.FunctionState = functionWait.FunctionState;
    //        if (functionParentWait.IsFirst)
    //        {
    //            await _context.SaveChangesAsync();
    //            await RegisterFirstWait(functionParentWait.RequestedByFunction.MethodInfo);
    //        }
    //        await ProceedToNextWait(functionParentWait);
    //    }
    //}
}
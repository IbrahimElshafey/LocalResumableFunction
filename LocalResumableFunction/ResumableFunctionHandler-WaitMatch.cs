using System.Diagnostics;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    /// <summary>
    ///     When method called and finished
    /// </summary>
    internal async Task MethodCalled(PushedMethod pushedMethod)
    {
        try
        {
            var methodId = await _metodIdsRepo.GetMethodIdentifier(pushedMethod.MethodInfo);
            if (methodId.ExistInDb is false)
                //_context.MethodIdentifiers.Add(methodId.MethodIdentifier);
                throw new Exception(
                    $"Method [{pushedMethod.MethodInfo.Name}] is not registered in current database as [WaitMethod].");
            pushedMethod.MethodIdentifier = methodId.MethodIdentifier;
            var matchedWaits = await _waitsRepository.GetMatchedWaits(pushedMethod);
            foreach (var matchedWait in matchedWaits)
            {
                UpdateFunctionData(matchedWait, pushedMethod);
                //await HandleMatchedWaitNew(matchedWait);
                await HandleMatchedWait(matchedWait);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
        }
    }


    private async Task HandleMatchedWaitNew(MethodWait matchedWait)
    {

        bool getNextWait = false;
        Wait previousChild = null;
        Wait wait = matchedWait;
        do
        {
            switch (wait)
            {
                case MethodWait methodWait:
                    methodWait.Status = WaitStatus.Completed;
                    getNextWait = true;
                    break;

                case ManyMethodsWait { WaitType: WaitType.AllMethodsWait } allMethodsWait:
                    allMethodsWait = await _waitsRepository.ReloadChildWaits(allMethodsWait);
                    allMethodsWait.MoveToMatched(previousChild);
                    getNextWait = allMethodsWait.Status == WaitStatus.Completed;
                    break;

                case ManyMethodsWait { WaitType: WaitType.AnyMethodWait } anyMethodWait:
                    anyMethodWait = await _waitsRepository.ReloadChildWaits(anyMethodWait);
                    anyMethodWait.SetMatchedMethod(previousChild);
                    getNextWait = true;
                    break;

                case FunctionWait:
                    getNextWait = true;
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AllFunctionsWait } allFunctionsWait:
                    allFunctionsWait.MoveToMatched(previousChild.Id);
                    getNextWait = allFunctionsWait.Status == WaitStatus.Completed;
                    break;

                case ManyFunctionsWait { WaitType: WaitType.AnyFunctionWait } anyFunctionWait:
                    anyFunctionWait.SetMatchedFunction(previousChild.Id);
                    await _waitsRepository.CancelAllWaits(anyFunctionWait);
                    getNextWait = true;
                    break;
            }

            previousChild = wait;
            wait = await _waitsRepository.GetWaitParent(wait);
        } while (wait != null);

        if (getNextWait && previousChild != null)
        {
            var nextWaitResult = await previousChild.CurrntFunction.GetNextWait(previousChild);
            if (nextWaitResult is null) return;
            await HandleNextWaitResult(nextWaitResult, previousChild);
            matchedWait.FunctionState.StateObject = matchedWait.CurrntFunction;
            await _context.SaveChangesAsync();

            await DuplicateIfFirst(matchedWait);
        }
    }
    private async Task HandleMatchedWait(MethodWait matchedWait)
    {
        if (IsSingleMethod(matchedWait) || await IsGroupLastWait(matchedWait))
        {
            //get next Method wait
            var nextWaitResult = await matchedWait.CurrntFunction.GetNextWait(matchedWait);
            if (nextWaitResult is null) return;
            await HandleNextWaitResult(nextWaitResult, matchedWait);
            matchedWait.FunctionState.StateObject = matchedWait.CurrntFunction;
            await _context.SaveChangesAsync();

            await DuplicateIfFirst(matchedWait);
        }
    }

    private bool IsSingleMethod(MethodWait currentWait)
    {
        return currentWait.ParentWaitId is null;
    }

    private async Task<bool> IsGroupLastWait(MethodWait currentWait)
    {
        var group = await _waitsRepository.GetWaitGroup(currentWait.ParentWaitId);
        currentWait.ParentWait = group;
        switch (group)
        {
            case ManyMethodsWait allMethodsWait
                when group.WaitType == WaitType.AllMethodsWait:
                allMethodsWait.MoveToMatched(currentWait);
                return allMethodsWait.Status == WaitStatus.Completed;

            case ManyMethodsWait anyMethodWait
                when group.WaitType == WaitType.AnyMethodWait:
                anyMethodWait.SetMatchedMethod(currentWait);
                return true;
            case FunctionWait:
                return true;
        }

        return false;
    }

    private async Task<bool> HandleNextWaitResult(NextWaitResult nextWaitResult, Wait currentWait)
    {
        // return await HandleNextWait(nextWaitAftreBacktoCaller, lastFunctionWait, functionClass);
        if (IsFinalExit(nextWaitResult))
        {
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
            currentWait.FunctionState.IsCompleted = true;
            return await MoveFunctionToRecycleBin(currentWait);
        }

        if (IsSubFunctionExit(nextWaitResult)) return await SubFunctionExit(currentWait);

        return await HandleNextWait(nextWaitResult.Result, currentWait);
    }

    private async Task<bool> HandleNextWait(Wait nextWaitResult, Wait currentWait)
    {
        if (nextWaitResult is null) return false;

        //this may cause and error in case of 
        nextWaitResult.ParentWaitId = currentWait.ParentWaitId;
        nextWaitResult.FunctionState = currentWait.FunctionState;
        nextWaitResult.RequestedByFunctionId = currentWait.RequestedByFunctionId;
        if (nextWaitResult is ReplayWait replayWait)
            return await ReplayWait(replayWait);

        currentWait.Status = WaitStatus.Completed;
        return await GenericWaitRequested(nextWaitResult);
    }

    private bool IsFinalExit(NextWaitResult nextWait)
    {
        return nextWait.Result is null && nextWait.IsFinalExit;
    }

    private bool IsSubFunctionExit(NextWaitResult nextWait)
    {
        return nextWait.Result is null && nextWait.IsSubFunctionExit;
    }

    private async Task<bool> SubFunctionExit(Wait lastFunctionWait)
    {
        Debugger.Launch();
        //lastFunctionWait =  last function wait before exsit
        var rootFunctionResult = await _waitsRepository.GetRootFunctionWait(lastFunctionWait.ParentWaitId);
        var rootFunctionWait = rootFunctionResult.RootWait;
        if (rootFunctionWait == null)
        {
            WriteMessage(
                $"Root function wait not exist for wait ({lastFunctionWait.Name}) with type ({lastFunctionWait.WaitType}).");
            return false;
        }

        if (rootFunctionWait.Status != WaitStatus.Waiting)
        {
            WriteMessage(
                $"The status for parent function wait ({rootFunctionWait.Name}) must be ({WaitStatus.Waiting}).");
            return false;
        }

        var backToCaller = false;
        lastFunctionWait.Status = WaitStatus.Completed;
        Debugger.Launch();
        switch (rootFunctionWait)
        {
            //one sub function -> return to caller after function end
            case FunctionWait:
                backToCaller = true;
                break;
            //many sub functions -> wait function group to complete and return to caller
            case ManyFunctionsWait allFunctionsWait
                when rootFunctionWait.WaitType == WaitType.AllFunctionsWait:
                allFunctionsWait.MoveToMatched(rootFunctionResult.FunctionWaitId);
                if (allFunctionsWait.Status == WaitStatus.Completed)
                    backToCaller = true;
                break;
            case ManyFunctionsWait anyFunctionWait
                when rootFunctionWait.WaitType == WaitType.AnyFunctionWait:
                anyFunctionWait.SetMatchedFunction(rootFunctionResult.FunctionWaitId);
                await _waitsRepository.CancelAllWaits(anyFunctionWait);
                if (anyFunctionWait.Status == WaitStatus.Completed)
                    backToCaller = true;
                break;
            default:
                WriteMessage(
                    $"Root function wait ({rootFunctionWait.Name}) with type ({rootFunctionWait.WaitType}) is not valid function wait.");
                break;
        }

        if (backToCaller)
        {
            rootFunctionWait.Status = WaitStatus.Completed;
            var nextWaitAfterBackToCaller = await rootFunctionWait.CurrntFunction.GetNextWait(rootFunctionWait);
            if (rootFunctionWait.IsFirst)
            {
                await _context.SaveChangesAsync();
                await RegisterFirstWait(rootFunctionWait.RequestedByFunction.MethodInfo);
            }

            return await HandleNextWaitResult(nextWaitAfterBackToCaller, rootFunctionWait);
        }

        return true;
    }
}
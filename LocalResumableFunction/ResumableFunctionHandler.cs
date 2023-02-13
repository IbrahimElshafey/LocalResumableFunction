using System.Reflection;
using LocalResumableFunction.Data;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal class ResumableFunctionHandler
{
    private EngineDataContext _context;
    private WaitsRepository _waitsRepository;

    internal ResumableFunctionHandler(EngineDataContext? context = null)
    {
        _context = context ?? new EngineDataContext();
        _waitsRepository = new WaitsRepository(_context);
    }
    /// <summary>
    ///     When method called and finished
    /// </summary>
    internal async Task MethodCalled(PushedMethod pushedMethod)
    {
        var matchedWaits = await _waitsRepository.GetMatchedWaits(pushedMethod);
        foreach (var currentWait in matchedWaits)
        {
            UpdateFunctionData(currentWait, pushedMethod);
            await HandlePushedMethod(currentWait);
            //await _functionRepository.SaveFunctionState(currentWait.FunctionRuntimeInfo);
            await _context.SaveChangesAsync();
        }

        foreach (var currentWait in matchedWaits)
        {
            //check if pushed Method is matched against waits
            //currentWait.UpdateFunctionData();
            //get next wait if (IsSingleMethod(currentWait) || await IsGroupLastWait(currentWait))
            //load state and status from database
        }
    }

    

    private async Task HandlePushedMethod(MethodWait currentWait)
    {
        if (IsSingleMethod(currentWait) || await IsGroupLastWait(currentWait))
        {
            //get next Method wait
            var nextWaitResult = await currentWait.CurrntFunction.GetNextWait(currentWait);
            await HandleNextWait(nextWaitResult, currentWait);
            await _waitsRepository.DuplicateWaitIfFirst(currentWait);
        }

        currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
    }

    private void UpdateFunctionData(MethodWait currentWait, PushedMethod pushedMethod)
    {
        var setDataExpression = currentWait.SetDataExpression.Compile();
        setDataExpression.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, currentWait.CurrntFunction);
    }

    private bool IsSingleMethod(MethodWait currentWait)
    {
        return currentWait.ParentWaitsGroupId is null;
    }

    private async Task<bool> IsGroupLastWait(MethodWait currentWait)
    {
        var group = await _waitsRepository.GetWaitGroup(currentWait.ParentWaitsGroupId);
        switch (group)
        {
            case ManyMethodsWait allMethodsWait
                when group.WaitType == WaitType.AllMethodsWait:
                allMethodsWait.MoveToMatched(currentWait);
                return allMethodsWait.Status == WaitStatus.Completed;

            case ManyMethodsWait anyMethodWait
                when group.WaitType == WaitType.AnyMethodWait:
                anyMethodWait.SetMatchedMethod(currentWait);
                ;
                return true;
        }

        return false;
    }

    private async Task<bool> HandleNextWait(NextWaitResult nextWaitResult, Wait currentWait)
    {
        // return await HandleNextWait(nextWaitAftreBacktoCaller, lastFunctionWait, functionClass);
        if (IsFinalExit(nextWaitResult))
        {
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
            return await MoveFunctionToRecycleBin(currentWait);
        }

        if (IsSubFunctionExit(nextWaitResult)) return await SubFunctionExit(currentWait);

        if (nextWaitResult.Result is not null)
        {
            //this may cause and error in case of 
            nextWaitResult.Result.ParentWaitId = currentWait.ParentWaitId;
            if (nextWaitResult.Result is ReplayWait replayWait)
                return await ReplayWait(replayWait);

            //nextWaitResult.Result.FunctionId = currentWait.FunctionId;
            nextWaitResult.Result.FunctionState = currentWait.FunctionState;
            var result = await GenericWaitRequested(nextWaitResult.Result);
            currentWait.Status = WaitStatus.Completed;
            return result;
        }

        return false;
    }

    private async Task<bool> MoveFunctionToRecycleBin(Wait currentWait)
    {
        //throw new NotImplementedException();
        return true;
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
        //lastFunctionWait =  last function wait before exsit
        var parentFunctionWait = await _waitsRepository.GetParentFunctionWait(lastFunctionWait.ParentWaitId);
        var backToCaller = false;
        switch (parentFunctionWait)
        {
            //one sub function -> return to caller after function end
            case FunctionWait:
                backToCaller = true;
                break;
            //many sub functions -> wait function group to complete and return to caller
            case ManyFunctionsWait allFunctionsWait
                when parentFunctionWait.WaitType == WaitType.AllFunctionsWait:
                allFunctionsWait.MoveToMatched(lastFunctionWait.ParentWaitId);
                if (allFunctionsWait.Status == WaitStatus.Completed)
                    backToCaller = true;
                break;
            case ManyFunctionsWait anyFunctionWait
                when parentFunctionWait.WaitType == WaitType.AnyFunctionWait:
                anyFunctionWait.SetMatchedFunction(lastFunctionWait.ParentWaitId);
                if (anyFunctionWait.Status == WaitStatus.Completed)
                    backToCaller = true;
                break;
        }

        if (backToCaller)
        {
            var nextWaitAftreBacktoCaller = await parentFunctionWait.CurrntFunction.GetNextWait(parentFunctionWait);
            //HandleNextWait after back to caller
            return await HandleNextWait(nextWaitAftreBacktoCaller, lastFunctionWait);
        }

        return true;
    }

    private async Task<bool> ReplayWait(ReplayWait replayWait)
    {
        switch (replayWait.ReplayType)
        {
            case ReplayType.ExecuteNoWait:
                await Replay(replayWait);
                break;
            case ReplayType.WaitAgain:
                //oldCompletedWait.ReplayType = null;
                await GenericWaitRequested(replayWait);
                break;
            default:
                throw new Exception("ReplayWait exception.");
        }

        return true;
    }

    private async Task Replay(Wait oldCompletedWait)
    {
        var nextWaitResult = await oldCompletedWait.CurrntFunction.GetNextWait(oldCompletedWait);
        await HandleNextWait(nextWaitResult, oldCompletedWait);
    }

    internal async Task<bool> GenericWaitRequested(Wait newWait)
    {
        newWait.Status = WaitStatus.Waiting;
        if (Validate(newWait) is false) return false;
        switch (newWait)
        {
            case MethodWait methodWait:
                await SingleWaitRequested(methodWait);
                break;
            case ManyMethodsWait manyWaits:
                await ManyWaitsRequested(manyWaits);
                break;
            case FunctionWait functionWait:
                await FunctionWaitRequested(functionWait);
                break;
            case ManyFunctionsWait manyFunctionsWait:
                await ManyFunctionsWaitRequested(manyFunctionsWait);
                break;
        }

        return false;
    }

    private async Task SingleWaitRequested(MethodWait methodWait)
    {
        var repo = new MethodIdentifierRepository(_context);
        var waitMethodIdentifier = await repo.GetMethodIdentifier(methodWait.WaitMethodIdentifier);
        methodWait.WaitMethodIdentifier = waitMethodIdentifier;
        methodWait.WaitMethodIdentifierId = waitMethodIdentifier.Id;
        await _waitsRepository.AddWait(methodWait);
    }

    private async Task ManyWaitsRequested(ManyMethodsWait manyWaits)
    {
        foreach (var methodWait in manyWaits.WaitingMethods)
        {
            methodWait.Status = WaitStatus.Waiting;
            await SingleWaitRequested(methodWait);
        }

        await _waitsRepository.AddWait(manyWaits);
    }

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        if (functionWait.FirstWait is ReplayWait replayWait)
            await ReplayWait(replayWait);
        else
            await GenericWaitRequested(functionWait.FirstWait);
        await _waitsRepository.AddWait(functionWait);
    }

    private async Task ManyFunctionsWaitRequested(ManyFunctionsWait functionsWait)
    {
        foreach (var functionWait in functionsWait.WaitingFunctions)
        {
            functionsWait.Status = WaitStatus.Waiting;
            await FunctionWaitRequested(functionWait);
        }

        await _waitsRepository.AddWait(functionsWait);
    }

    private bool Validate(Wait nextWait)
    {
        return true;
    }

    internal static async Task RegisterFirstWaits(string[]? assemblyNames)
    {
    }


    internal static MethodInfo GetMethodInfo(MethodIdentifier methodIdentifier)
    {
        throw new NotImplementedException();
    }
}
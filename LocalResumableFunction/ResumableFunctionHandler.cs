using System.Diagnostics;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private readonly FunctionDataContext _context;
    private readonly WaitsRepository _waitsRepository;
    private readonly MethodIdentifierRepository _metodIdsRepo;

    internal ResumableFunctionHandler(FunctionDataContext? context = null)
    {
        _context = context ?? new FunctionDataContext();
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
    }

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
            foreach (var currentWait in matchedWaits)
            {
                UpdateFunctionData(currentWait, pushedMethod);
                await HandlePushedMethod(currentWait);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
        }
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


    private async Task HandlePushedMethod(MethodWait currentWait)
    {
        if (IsSingleMethod(currentWait) || await IsGroupLastWait(currentWait))
        {
            //get next Method wait
            var nextWaitResult = await currentWait.CurrntFunction.GetNextWait(currentWait);
            if (nextWaitResult is null) return;
            await HandleNextWait(nextWaitResult, currentWait);
            currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
            await _context.SaveChangesAsync();

            await DuplicateIfFirst(currentWait);
        }
    }

    private async Task DuplicateIfFirst(MethodWait currentWait)
    {
        Wait? wait = null;
        if (currentWait.IsFirst)
            wait = currentWait;
        else if (currentWait?.ParentWaitsGroup?.IsFirst == true)
            wait = currentWait.ParentWaitsGroup;
        if (wait != null)
            await RegisterFirstWait(wait.RequestedByFunction.MethodInfo);
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
        currentWait.ParentWaitsGroup = group;
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
            nextWaitResult.Result.FunctionState = currentWait.FunctionState;
            nextWaitResult.Result.RequestedByFunctionId = currentWait.RequestedByFunctionId;
            if (nextWaitResult.Result is ReplayWait replayWait)
                return await ReplayWait(replayWait);


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
        if (parentFunctionWait == null)
        {
            WriteMessage($"Parent function wait not exist for wait ({lastFunctionWait.Name}) with type ({lastFunctionWait.WaitType}).");
            return false;
        }

        if (parentFunctionWait.Status != WaitStatus.Waiting)
        {
            WriteMessage($"The status for parent function wait ({parentFunctionWait.Name}) must be ({WaitStatus.Waiting}).");
            return false;
        }
        var backToCaller = false;
        lastFunctionWait.Status = WaitStatus.Completed;
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
                await  _waitsRepository.CancelAllWaits(anyFunctionWait);
                if (anyFunctionWait.Status == WaitStatus.Completed)
                    backToCaller = true;
                break;
        }

        if (backToCaller)
        {
            parentFunctionWait.Status = WaitStatus.Completed;
            var nextWaitAftreBacktoCaller = await parentFunctionWait.CurrntFunction.GetNextWait(parentFunctionWait);
            if (parentFunctionWait.IsFirst)
            {
                await _context.SaveChangesAsync();
                await RegisterFirstWait(parentFunctionWait.RequestedByFunction.MethodInfo);
            }
            return await HandleNextWait(nextWaitAftreBacktoCaller, parentFunctionWait);
        }

        return true;
    }

    private async Task SingleWaitRequested(MethodWait methodWait)
    {
        var waitMethodIdentifier = await _metodIdsRepo.GetMethodIdentifier(methodWait.WaitMethodIdentifier);
        methodWait.WaitMethodIdentifier = waitMethodIdentifier;
        methodWait.WaitMethodIdentifierId = waitMethodIdentifier.Id;
        methodWait.SetExpressions();
        
        await _waitsRepository.AddWait(methodWait);
    }


    private async Task ManyWaitsRequested(ManyMethodsWait manyWaits)
    {
        for (var index = 0; index < manyWaits.WaitingMethods.Count; index++)
        {
            var methodWait = manyWaits.WaitingMethods[index];
            methodWait.Status = WaitStatus.Waiting;
            methodWait.FunctionState = manyWaits.FunctionState;
            methodWait.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            methodWait.StateAfterWait = manyWaits.StateAfterWait;
            methodWait.ParentWaitId = manyWaits.ParentWaitId;
            await SingleWaitRequested(methodWait);
        }

        await _waitsRepository.AddWait(manyWaits);
    }

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        await _waitsRepository.AddWait(functionWait);
        await _context.SaveChangesAsync();

        var functionRunner = new FunctionRunner(functionWait.CurrntFunction, functionWait.FunctionInfo);
        var hasNext = await functionRunner.MoveNextAsync();
        functionWait.FirstWait = functionRunner.Current;
        if (hasNext is false)
        {
            WriteMessage($"No waits exist in sub function ({functionWait.FunctionInfo.Name})");
            return;
        }

        functionWait.FirstWait = functionRunner.Current;
        //functionWait.FirstWait.StateAfterWait = functionRunner.GetState();
        functionWait.FirstWait.FunctionState = functionWait.FunctionState;
        functionWait.FirstWait.FunctionStateId = functionWait.FunctionState.Id;
        functionWait.FirstWait.ParentWait = functionWait;
        functionWait.FirstWait.ParentWaitId = functionWait.Id;
        var methodIdentifier = await _metodIdsRepo.GetMethodIdentifier(functionWait.FunctionInfo);
        functionWait.FirstWait.RequestedByFunction = methodIdentifier.MethodIdentifier;
        functionWait.FirstWait.RequestedByFunctionId = methodIdentifier.MethodIdentifier.Id;

        if (functionWait.FirstWait is ReplayWait replayWait)
            await ReplayWait(replayWait);
        else
            await GenericWaitRequested(functionWait.FirstWait);
    }

    private async Task ManyFunctionsWaitRequested(ManyFunctionsWait manyFunctionsWaits)
    {
        foreach (var functionWait in manyFunctionsWaits.WaitingFunctions)
        {
            functionWait.Status = WaitStatus.Waiting;
            functionWait.FunctionState = manyFunctionsWaits.FunctionState;
            functionWait.RequestedByFunctionId = manyFunctionsWaits.RequestedByFunctionId;
            functionWait.StateAfterWait = manyFunctionsWaits.StateAfterWait;
            functionWait.ParentWaitId = manyFunctionsWaits.ParentWaitId;
        }

        await _waitsRepository.AddWait(manyFunctionsWaits);
        await _context.SaveChangesAsync();

        foreach (var functionWait in manyFunctionsWaits.WaitingFunctions)
        {
            try
            {
                await FunctionWaitRequested(functionWait);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private bool Validate(Wait nextWait)
    {
        return true;
    }
}
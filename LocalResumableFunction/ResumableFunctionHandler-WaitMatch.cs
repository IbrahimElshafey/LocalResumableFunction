﻿using System.Collections.Concurrent;
using System.Diagnostics;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    /// <summary>
    ///     When method called and finished
    /// </summary>

    private static readonly ConcurrentQueue<PushedMethod> PushedMethods = new();
    private static readonly ConcurrentBag<MethodWait> PendingWaits = new();

    internal async Task MethodCalled(PushedMethod pushedMethod)
    {
        PushedMethods.Enqueue(pushedMethod);
        await Processing();
    }

    internal async Task Processing()
    {
       
        for (var i = 0; i < PushedMethods.Count; i++)
        {
            if (PushedMethods.TryDequeue(out PushedMethod current))
                await Process(current);
        }
        for (int i = 0; i < PendingWaits.Count; i++)
        {
            if (PendingWaits.TryTake(out var pendingWait))
                await ProcessWait(pendingWait);
        }

        async Task Process(PushedMethod pushedMethod)
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
                    await ProcessWait(matchedWait);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }
    }

    private async Task ProcessWait(MethodWait matchedWait)
    {
        if (await CanProcessNow(matchedWait) is false) return;
        matchedWait.UpdateFunctionData();
        await HandleMatchedWaitNew(matchedWait);
        await ProcessingFinished(matchedWait);
    }


    private async Task<bool> CanProcessNow(MethodWait matchedWait)
    {
        if (matchedWait.FunctionState.IsInProcessing)
        {
            PendingWaits.Add(matchedWait);
            return false;
        }
        matchedWait.FunctionState.IsInProcessing = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ProcessingFinished(MethodWait matchedWait)
    {
        if (PendingWaits.Contains(matchedWait))
        {
            PendingWaits.TryTake(out matchedWait);
        }
        matchedWait.FunctionState.IsInProcessing = false;
        await _context.SaveChangesAsync();
    }


    private async Task HandleMatchedWait(MethodWait matchedWait)
    {
        if (IsSingleMethod(matchedWait) || await IsGroupLastWait(matchedWait))
        {
            //get next Method wait
            var nextWaitResult = await matchedWait.GetNextWait();
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
        if (nextWaitResult.IsFinalExit())
        {
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
            currentWait.FunctionState.IsCompleted = true;
            return await MoveFunctionToRecycleBin(currentWait);
        }

        if (nextWaitResult.IsSubFunctionExit()) return await SubFunctionExit(currentWait);

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
                await _waitsRepository.CancelFunctionGroupWaits(anyFunctionWait);
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
            var nextWaitAfterBackToCaller = await rootFunctionWait.GetNextWait();
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
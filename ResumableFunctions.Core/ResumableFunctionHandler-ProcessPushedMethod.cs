using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using ResumableFunctions.Core.Abstraction;

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler : IPushMethodCall, IResumableFunctionsReceiver
{
    /// <summary>
    ///     When method called and finished
    /// </summary>
    public void MethodCalled(PushedMethod pushedMethod)
    {
        _backgroundJobClient.Enqueue(() => ProcessPushedMethod(pushedMethod));
    }

    public async Task ProcessPushedMethod(PushedMethod pushedMethod)
    {
        //todo:move this code to background task
        /*
         * Notify cuurent service HandleWaitsBackground
         * Save `pushedMethod` to database table
         * GetMethodActiveWaits
         * Save active waits to ActiveWaits table (wait id, pushed method id, AssemblyName, status)
         * Handle this list one by one
         * If wait owned by current service,Handle local 
         * If external notify the other service with ID
         */
        try
        {
            Debugger.Launch();
            var matchedWaits = await _waitsRepository.GetMethodActiveWaits(pushedMethod);
            if (matchedWaits?.Any() is true)
            {
                await _context.PushedMethodsCalls.AddAsync(pushedMethod);
            }
            foreach (var methodWait in matchedWaits)
            {
                var isLocalWait =
                    methodWait.RequestedByFunction.AssemblyName ==
                    Assembly.GetEntryAssembly().GetName().Name;//Todo:get from "ServiceName" in config
                if (isLocalWait)
                {
                    //handle if local
                    await ProcessMatchedWait(methodWait, pushedMethod);
                }
                else
                {
                    //todo: call "api/MatchedWaitReceiver/WaitMatched" for the other service
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
            WriteMessage(ex.Message);
        }
    }

    private async Task<bool> CheckIfMatch(PushedMethod pushedMethod, MethodWait methodWait)
    {
        methodWait.LoadExpressions();
        methodWait.Input = pushedMethod.Input;
        methodWait.Output = pushedMethod.Output;
        switch (methodWait.NeedFunctionStateForMatch)
        {
            case false when methodWait.IsMatched():
                await LoadWaitFunctionState(methodWait);
                return true;

            case true:
                await LoadWaitFunctionState(methodWait);
                if (methodWait.IsMatched())
                    return true;
                break;
        }

        return false;

        async Task LoadWaitFunctionState(MethodWait wait)
        {
            wait.FunctionState = await _context.FunctionStates.FindAsync(wait.FunctionStateId);
        }
    }

    public void WaitMatched(int waitId, int pushedMethodId)
    {
        _backgroundJobClient.Enqueue(() => ProcessMatchedWait(waitId, pushedMethodId));
    }

    public async Task ProcessMatchedWait(int waitId, int pushedMethodId)
    {
        //_context
        //_backgroundJobClient
        var methodWait = await _context
            .MethodWaits
            .Include(x => x.RequestedByFunction)
            .Where(x => x.Status == WaitStatus.Waiting)
            .FirstAsync(x => x.Id == waitId);
        var pushedMethod = await _context
           .PushedMethodsCalls
           .FirstAsync(x => x.Id == pushedMethodId);
        //todo:convert pushed method input and output 
        //Get MethodInfo and use it
        //If assembly name not the current then search for external methods marked with [ExternalWaitMethodAttribute] that match
        await ProcessMatchedWait(methodWait, pushedMethod);
    }

    private async Task ProcessMatchedWait(MethodWait methodWait, PushedMethod pushedMethod)
    {
        if (!await CheckIfMatch(pushedMethod, methodWait))
            return;
        //todo:cancel processing and rewait it if data is locked
        methodWait.UpdateFunctionData();
        await ResumeExecution(methodWait);
        await _context.SaveChangesAsync();
    }
}
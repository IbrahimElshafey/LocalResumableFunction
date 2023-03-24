using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler 
{
    /// <summary>
    ///     When method called and finished
    /// </summary>


    

    private async Task<bool> CheckIfMatch(MethodWait methodWait)
    {
        methodWait.LoadExpressions();
        //methodWait.Input = pushedMethod.Input;
        //methodWait.Output = pushedMethod.Output;
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

  

    private async Task ProcessMatchedWait(MethodWait methodWait)
    {
        if (!await CheckIfMatch(methodWait))
            return;
        //todo:cancel processing and rewait it if data is locked
        methodWait.UpdateFunctionData();
        await ResumeExecution(methodWait);
        await _context.SaveChangesAsync();
    }
}
﻿using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    protected async Task<FunctionWait> Function(string name, Func<IAsyncEnumerable<Wait>> function)
    {
        var result = new FunctionWait
        {
            Name = name,
            IsNode = true,
            WaitType = WaitType.FunctionWait
        };
        var asyncEnumerator = function().GetAsyncEnumerator();
        await asyncEnumerator.MoveNextAsync();
        var firstWait = asyncEnumerator.Current;
        //firstWait.ParentFunctionWaitId = result.Id;
        result.FirstWait = firstWait;
        //result.InitiatedByFunctionName = result.FunctionName;
        return result;
    }

    protected async Task<ManyFunctionsWait> Functions
        (string name, Func<IAsyncEnumerable<Wait>>[] subFunctions)
    {
        var result = new ManyFunctionsWait
        {
            WaitingFunctions = new List<FunctionWait>(subFunctions.Length),
            Name = name,
            IsNode = true,
            WaitType = WaitType.AllFunctionsWait
        };
        for (var i = 0; i < subFunctions.Length; i++)
        {
            var currentFunction = subFunctions[i];
            var currentFuncResult = await Function("", currentFunction);
            currentFuncResult.IsNode = false;
            currentFuncResult.FirstWait.ParentWaitId = result.Id;
            currentFuncResult.ParentFunctionGroupId = result.Id;
            result.WaitingFunctions[i] = currentFuncResult;
        }

        return result;
    }
}
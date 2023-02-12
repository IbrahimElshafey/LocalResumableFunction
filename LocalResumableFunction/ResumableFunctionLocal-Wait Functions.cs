using LocalResumableFunction.InOuts;
using Newtonsoft.Json;
using LocalResumableFunction;
using System.Xml.XPath;

public abstract partial class ResumableFunctionLocal
{
    protected async Task<FunctionWait> Function(string name, Func<IAsyncEnumerable<Wait>> function)
    {
        var result = new FunctionWait(name, function)
        {
            RequestedByFunction = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall(),
            IsNode = true,
            WaitType = WaitType.FunctionWait
        };
        var asyncEnumerator = function().GetAsyncEnumerator();
        await asyncEnumerator.MoveNextAsync();
        var firstWait = asyncEnumerator.Current;
        firstWait.ParentFunctionWaitId = result.Id;
        result.CurrentWait = firstWait;
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
            RequestedByFunction = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall(),
            IsNode = true,
        };
        for (int i = 0; i < subFunctions.Length; i++)
        {
            var currentFunction = subFunctions[i];
            var currentFuncResult = await Function("", currentFunction);
            currentFuncResult.RequestedByFunction = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall();
            currentFuncResult.IsNode = false;
            currentFuncResult.CurrentWait.ParentFunctionWaitId = result.Id;
            currentFuncResult.ParentFunctionGroupId = result.Id;
            result.WaitingFunctions[i] = currentFuncResult;
        }
        return result;
    }


}

using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    protected FunctionWait WaitFunction(string name, Func<IAsyncEnumerable<Wait>> function)
    {
        var result = new FunctionWait
        {
            Name = name,
            IsNode = true,
            WaitType = WaitType.FunctionWait,
            FunctionInfo = function.Method
        };
        return result;
    }

    protected ManyFunctionsWait WaitFunctions
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
            var currentFuncResult = WaitFunction($"#{currentFunction.Method.Name}#", currentFunction);
            currentFuncResult.IsNode = false;
            currentFuncResult.ParentFunctionGroupId = result.Id;
            result.WaitingFunctions[i] = currentFuncResult;
        }

        return result;
    }
}
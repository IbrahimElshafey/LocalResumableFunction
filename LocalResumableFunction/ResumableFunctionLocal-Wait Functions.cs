using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    protected FunctionWait Wait(string name, Func<IAsyncEnumerable<Wait>> function)
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

    protected ManyFunctionsWait Wait
        (string name, params Func<IAsyncEnumerable<Wait>>[] subFunctions)
    {
        var result = new ManyFunctionsWait
        {
            ChildWaits = new List<Wait>(new Wait[subFunctions.Length]),
            Name = name,
            IsNode = true,
            WaitType = WaitType.AllFunctionsWait
        };
        for (var index = 0; index < subFunctions.Length; index++)
        {
            var currentFunction = subFunctions[index];
            var currentFuncResult = Wait($"#{currentFunction.Method.Name}#", currentFunction);
            currentFuncResult.IsNode = false;
            currentFuncResult.ParentWait = result;
            result.ChildWaits[index] = currentFuncResult;
        }

        return result;
    }
}
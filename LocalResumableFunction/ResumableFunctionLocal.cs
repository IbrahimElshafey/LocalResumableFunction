using LocalResumableFunction.InOuts;
using Newtonsoft.Json;
using LocalResumableFunction;
using System.Xml.XPath;

public abstract class ResumableFunctionLocal
{
    [JsonExtensionData]
    public Dictionary<string, object> State { get; set; }

    public MethodWait<Input, Output> When<Input, Output>(string name, Func<Input, Output> method)
    {
        var result = new MethodWait<Input, Output>(method)
        {
            Name = name,
            //CurrntFunction = this,
            IsSingle = true,
            IsNode = true,
        };
        return result;
    }

    public ManyMethodsWait When(string name, params MethodWait[] manyMethodsWait)
    {
        var result = new ManyMethodsWait
        {
            Name = name,
            WaitingMethods = manyMethodsWait.ToList(),
            IsSingle = false,
            IsNode = true
        };
        foreach (var item in result.WaitingMethods)
        {
            item.ParentWaitsGroupId = result.Id;
            item.IsNode = false;
        }
        return result;
    }

    internal async Task<NextWaitResult> GetNextWait(Wait currentWait)
    {
        var functionRunner = new MethodRunner(currentWait);
        if (functionRunner is null)
            throw new Exception(
                $"Can't initiate runner");
        try
        {
            var waitExist = await functionRunner.MoveNextAsync();
            if (waitExist)
            {
                functionRunner.Current.StateAfterWait = functionRunner.GetState();
                return new NextWaitResult(functionRunner.Current, false, false);
            }
            else
            {
                //if current Function runner name is the main function start
                if (currentWait.ParentFunctionWaitId == null)
                {
                    return new NextWaitResult(null, true, false);
                }
                return new NextWaitResult(null, false, true);
            }
        }
        catch (Exception)
        {

            throw new Exception("Error when try to get next wait");
        }
    }




    protected async Task<FunctionWait> Function(string name, Func<IAsyncEnumerable<Wait>> function)
    {
        var result = new FunctionWait(name, function)
        {
            InitiatedByMethod = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall(),
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
            InitiatedByMethod = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall(),
            IsNode = true,
        };
        for (int i = 0; i < subFunctions.Length; i++)
        {
            var currentFunction = subFunctions[i];
            var currentFuncResult = await Function("", currentFunction);
            currentFuncResult.InitiatedByMethod = LocalResumableFunction.Helpers.Extensions.CurrentResumableFunctionCall();
            currentFuncResult.IsNode = false;
            currentFuncResult.CurrentWait.ParentFunctionWaitId = result.Id;
            currentFuncResult.ParentFunctionGroupId = result.Id;
            result.WaitingFunctions[i] = currentFuncResult;
        }
        return result;
    }


}

using LocalResumableFunction;
using LocalResumableFunction.InOuts;
using Newtonsoft.Json;

public abstract partial class ResumableFunctionLocal
{
    [JsonExtensionData] public Dictionary<string, object> FunctionData { get; set; }


    public MethodWait<TInput, TOutput> When<TInput, TOutput>(string name, Func<TInput, TOutput> method)
    {
        var result = new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true
        };
        return result;
    }

    public ManyMethodsWait When(string name, params MethodWait[] manyMethodsWait)
    {
        var result = new ManyMethodsWait
        {
            Name = name,
            WaitingMethods = manyMethodsWait.ToList(),
            IsNode = true,
            WaitType = WaitType.AllMethodsWait
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
        var functionRunner = new FunctionRunner(currentWait);
        if (functionRunner is null)
            throw new Exception(
                "Can't initiate runner");
        try
        {
            var waitExist = await functionRunner.MoveNextAsync();
            if (waitExist)
            {
                functionRunner.Current.StateAfterWait = functionRunner.GetState();
                return new NextWaitResult(functionRunner.Current, false, false);
            }

            //if current Function runner name is the main function start
            if (currentWait.ParentWaitId == null) return new NextWaitResult(null, true, false);
            return new NextWaitResult(null, false, true);
        }
        catch (Exception)
        {
            throw new Exception("Error when try to get next wait");
        }
    }
}
using System.Diagnostics;
using LocalResumableFunction;
using LocalResumableFunction.InOuts;
using Newtonsoft.Json;

public abstract partial class ResumableFunctionLocal
{
    [JsonExtensionData] public Dictionary<string, object> FunctionData { get; set; }

    public MethodWait<TInput, TOutput> When<TInput, TOutput>(string name, Func<TInput, Task<TOutput>> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true
        };
    }

    public MethodWait<TInput, TOutput> When<TInput, TOutput>(string name, Func<TInput, TOutput> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true
        };
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

    internal async Task<NextWaitResult?> GetNextWait(Wait currentWait)
    {
        var functionRunner = new FunctionRunner(currentWait);
        if (functionRunner.ResumableFunctionExist is false)
        {
            Debug.WriteLine($"Resumable function ({currentWait.RequestedByFunction.MethodName}) not exist in code");
            //todo:delete it and all related waits
            //throw new Exception("Can't initiate runner");
            return null;
        }

        try
        {
            var waitExist = await functionRunner.MoveNextAsync();
            if (waitExist) return new NextWaitResult(functionRunner.Current, false, false);

            var isEntryFunctionEnd = currentWait.ParentWaitId == null;
            if (isEntryFunctionEnd)
                return new NextWaitResult(null, true, false);

            //sub function end
            return new NextWaitResult(null, false, true);
        }
        catch (Exception)
        {
            throw new Exception("Error when try to get next wait");
        }
    }
}
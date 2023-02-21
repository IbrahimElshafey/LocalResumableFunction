using System.Diagnostics;
using LocalResumableFunction;
using LocalResumableFunction.InOuts;
using Newtonsoft.Json;

public abstract partial class ResumableFunctionLocal
{
    [JsonExtensionData] public Dictionary<string, object> FunctionData { get; set; }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, Task<TOutput>> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true
        };
    }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, TOutput> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true
        };
    }

    protected ManyMethodsWait Wait(string name, params MethodWait[] manyMethodsWait)
    {
        var result = new ManyMethodsWait
        {
            Name = name,
            ChildWaits = manyMethodsWait.Select(x => (Wait)x).ToList(),
            IsNode = true,
            WaitType = WaitType.AllMethodsWait
        };
        foreach (var item in result.ChildWaits)
        {
            item.ParentWaitId = result.Id;
            item.IsNode = false;
        }

        return result;
    }
}
using System.Reflection;
using LocalResumableFunction.InOuts;
using Newtonsoft.Json;

namespace LocalResumableFunction;

public abstract partial class ResumableFunctionLocal
{

    internal MethodInfo CurrentResumableFunction { get; set; }
    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, Task<TOutput>> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true,
            CurrentFunction = this,
        };
    }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, TOutput> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            IsNode = true,
            CurrentFunction = this,
        };
    }

    protected WaitsGroup Wait(string name, params MethodWait[] manyMethodsWait)
    {
        var result = new WaitsGroup
        {
            Name = name,
            ChildWaits = manyMethodsWait.Select(x => (Wait)x).ToList(),
            IsNode = true,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        foreach (var item in result.ChildWaits)
        {
            item.ParentWaitId = result.Id;
            item.IsNode = false;
        }

        return result;
    }

    protected WaitsGroup Wait(string name, params Wait[] waits)
    {
        var result = new WaitsGroup
        {
            Name = name,
            ChildWaits = waits.ToList(),
            IsNode = true,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        foreach (var item in result.ChildWaits)
        {
            item.ParentWaitId = result.Id;
            item.IsNode = false;
        }

        return result;
    }
}
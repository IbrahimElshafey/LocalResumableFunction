using System.Reflection;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunction
{

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, TOutput> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
        };
    }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(string name, Func<TInput, Task<TOutput>> method)
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
        };
    }
    
    protected WaitsGroup Wait(string name, params Wait[] waits)
    {
        var result = new WaitsGroup
        {
            Name = name,
            ChildWaits = waits.ToList(),
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        foreach (var item in result.ChildWaits)
        {
            item.ParentWaitId = result.Id;
        }

        return result;
    }
}
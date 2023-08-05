using System.Reflection;
using System.Runtime.CompilerServices;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(
        Func<TInput, TOutput> method,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = ""
        )
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name ?? method.Method.Name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        };
    }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(
        Func<TInput, Task<TOutput>> method,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new MethodWait<TInput, TOutput>(method)
        {
            Name = name ?? method.Method.Name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        };
    }

    protected WaitsGroup Wait(string name, params Wait[] waits)
    {
        return new WaitsGroup
        {
            Name = name,
            ChildWaits = waits.ToList(),
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
    }
}
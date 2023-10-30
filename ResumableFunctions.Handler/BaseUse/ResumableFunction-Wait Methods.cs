using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        return new MethodWaitEntity<TInput, TOutput>(method)
        {
            Name = name ?? method.Method.Name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToMethodWait();
    }

    protected MethodWait<TInput, TOutput> Wait<TInput, TOutput>(
        Func<TInput, Task<TOutput>> method,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new MethodWaitEntity<TInput, TOutput>(method)
        {
            Name = name ?? method.Method.Name,
            WaitType = WaitType.MethodWait,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToMethodWait();
    }

    protected WaitsGroup Wait(string name, 
        Wait[] waits,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        var group = new WaitsGroupEntity
        {
            Name = name,
            ChildWaits = waits.Select(x => x.WaitEntity).ToList(),
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        };
        return group.ToWaitsGroup();
    }
}
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
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
            CallerName = callerName,
            Created = DateTime.UtcNow,
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
            CallerName = callerName,
            Created = DateTime.UtcNow
        }.ToMethodWait();
    }

    protected WaitsGroup Wait(
        Wait[] waits,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        if (waits.Any(x => x == null))
        {
            throw new ArgumentNullException($"The group wait named [{name}] contains wait that is null.");
        }
        var group = new WaitsGroupEntity
        {
            Name = name ?? "Group Wait",
            ChildWaits = waits.Select(x => x.WaitEntity).ToList(),
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName,
            Created = DateTime.UtcNow
        };
        return group.ToWaitsGroup();
    }
}
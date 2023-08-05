using ResumableFunctions.Handler.InOuts;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    protected TimeWait Wait(
        TimeSpan timeToWait,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new TimeWait(this)
        {
            Name = name ?? $"#{nameof(TimeWait)}#",
            TimeToWait = timeToWait,
            UniqueMatchId = Guid.NewGuid().ToString(),
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName,
        };
    }

}
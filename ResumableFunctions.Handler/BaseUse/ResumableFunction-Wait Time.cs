using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    protected TimeWait WaitTime(
        TimeSpan timeToWait,
        string name = null,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new TimeWaitEntity(this)
        {
            Name = name ?? $"#Time Wait for `{timeToWait.TotalHours}` hours in `{callerName}`",
            TimeToWait = timeToWait,
            UniqueMatchId = Guid.NewGuid().ToString(),
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName,
            Created = DateTime.UtcNow
        }.ToTimeWait();
    }

}
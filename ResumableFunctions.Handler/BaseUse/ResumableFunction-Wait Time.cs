using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts.Entities;
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
        return new TimeWaitEntity(this)
        {
            Name = name ?? $"#{nameof(TimeWaitEntity)}#",
            TimeToWait = timeToWait,
            UniqueMatchId = Guid.NewGuid().ToString(),
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName,
            Created = DateTime.Now
        }.ToTimeWait();
    }

}
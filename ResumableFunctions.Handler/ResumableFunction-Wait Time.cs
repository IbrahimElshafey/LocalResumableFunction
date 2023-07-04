using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunction
{
    protected TimeWait Wait(TimeSpan timeToWait, string name = null)
    {
        return new TimeWait
        {
            Name = name ?? $"#{nameof(TimeWait)}#",
            TimeToWait = timeToWait,
            UniqueMatchId = Guid.NewGuid().ToString(),
            CurrentFunction = this,
            IsNode = true,
        };
    }

}
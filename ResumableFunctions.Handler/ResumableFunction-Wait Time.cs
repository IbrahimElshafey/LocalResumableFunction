using System.Linq.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunction
{
    protected TimeWait Wait(TimeSpan timeToWait)
    {
        return new TimeWait
        {
            Name = ConstantValue.TimeWait,
            TimeToWait = timeToWait,
            UniqueMatchId = Guid.NewGuid().ToString(),
            CurrentFunction = this
        };
    }
    
}
using System.Linq.Expressions;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

public abstract partial class ResumableFunctionLocal
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
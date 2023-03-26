using System.Linq.Expressions;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.Core;

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
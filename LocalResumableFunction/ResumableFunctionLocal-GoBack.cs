using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    //todo:add GoBackTo
    protected ReplayWait GoBackAfter(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.GoAfter };
    }

    protected ReplayWait GoBackBefore(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.GoBefore };
    }

    protected ReplayWait GoBackBefore<TInput, TOutput>(string name, Expression<Func<TInput, TOutput, bool>> newMatchExpression)
    {
        return new ReplayWait
        {
            Name = name,
            ReplayType = ReplayType.GoBeforeWithNewMatch,
            MatchExpression = newMatchExpression
        };
    }
}
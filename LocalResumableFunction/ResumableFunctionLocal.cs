using System.Linq.Expressions;
using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    /// <summary>
    ///     Go back to code after the wait.
    /// </summary>
    protected ReplayWait GoBackAfter(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.GoAfter };
    }

    /// <summary>
    ///     Go back to code before the the wait and re-wait it again.
    /// </summary>
    protected ReplayWait GoBackBefore(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.GoBefore };
    }

    /// <summary>
    ///     Go back to code before the the wait and re-wait it again with new match condition.
    /// </summary>
    protected ReplayWait GoBackBefore<TInput, TOutput>(string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression)
    {
        return new ReplayWait
        {
            Name = name,
            ReplayType = ReplayType.GoBeforeWithNewMatch,
            MatchExpression = newMatchExpression
        };
    }

    protected ReplayWait GoBackTo(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.GoTo };
    }

    protected ReplayWait GoBackTo<TInput, TOutput>(string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression)
    {
        return new ReplayWait
        {
            Name = name,
            ReplayType = ReplayType.GoBeforeWithNewMatch,
            MatchExpression = newMatchExpression
        };
    }
}
using System.Linq.Expressions;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.Core;

public abstract partial class ResumableFunctionLocal
{
    /// <summary>
    ///     Go back to code after the wait.
    /// </summary>
    protected ReplayRequest GoBackAfter(string name)
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoAfter,
            CurrentFunction = this,
        };
    }

    /// <summary>
    ///     Go back to code before the wait and re-wait it again.
    /// </summary>
    protected ReplayRequest GoBackBefore(string name)
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoBefore,
            CurrentFunction = this
        };
    }

    /// <summary>
    ///     Go back to code before method wait and re-wait it again with new match condition.
    /// </summary>
    protected ReplayRequest GoBackBefore<TInput, TOutput>(string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression)
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoBeforeWithNewMatch,
            MatchExpression = newMatchExpression,
            CurrentFunction = this,
        };
    }

    /// <summary>
    ///     Go back to wait and re-wait it again.
    /// </summary>
    protected ReplayRequest GoBackTo(string name)
    {
        return new ReplayRequest { Name = name, ReplayType = ReplayType.GoTo };
    }

    /// <summary>
    ///     Go back to wait and re-wait it again with new match condition.
    /// </summary>
    protected ReplayRequest GoBackTo<TInput, TOutput>(string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression)
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoToWithNewMatch,
            MatchExpression = newMatchExpression,
            CurrentFunction = this,
        };
    }
}
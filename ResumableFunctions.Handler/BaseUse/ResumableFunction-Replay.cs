using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer : IObjectWithLog
{
    /// <summary>
    ///     Go back to code before method wait and re-wait it again with new match condition.
    /// </summary>
    protected Wait GoBackBefore<TInput, TOutput>(
        string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoBeforeWithNewMatch,
            MatchExpression = newMatchExpression,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToWait(); ;
    }


    /// <summary>
    ///     Go back to wait and re-wait it again with new match condition.
    /// </summary>
    protected Wait GoBackTo<TInput, TOutput>(
        string name,
        Expression<Func<TInput, TOutput, bool>> newMatchExpression,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoToWithNewMatch,
            MatchExpression = newMatchExpression,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToWait();
    }
}
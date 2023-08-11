using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer : IObjectWithLog
{
    /// <summary>
    ///     Go back to code after the wait.
    /// </summary>
    protected Wait GoBackAfter(
        string name,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoAfter,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToWait(); ;
    }

    /// <summary>
    ///     Go back to code before the wait and re-wait it again.
    /// </summary>
    protected Wait GoBackBefore(
        string name,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoBefore,
            CurrentFunction = this,
            InCodeLine = inCodeLine,
            CallerName = callerName
        }.ToWait(); ;
    }

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
    ///     Go back to wait and re-wait it again.
    /// </summary>
    protected Wait GoBackTo(
        string name,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new ReplayRequest
        {
            Name = name,
            ReplayType = ReplayType.GoTo,
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
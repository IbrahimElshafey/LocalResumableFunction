using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.BaseUse
{
    public class MethodWait<TInput, TOutput> : Wait
    {
        internal MethodWaitEntity<TInput, TOutput> MethodWaitEnitity { get; }

        internal MethodWait(MethodWaitEntity<TInput, TOutput> wait) : base(wait)
        {
            MethodWaitEnitity = wait;
        }

        public MethodWait<TInput, TOutput> AfterMatch(Action<TInput, TOutput> afterMatchAction)
        {
            MethodWaitEnitity.AfterMatch(afterMatchAction);
            return this;
        }

        public MethodWait<TInput, TOutput> MatchIf(Expression<Func<TInput, TOutput, bool>> matchExpression)
        {
            MethodWaitEnitity.MatchIf(matchExpression);
            return this;
        }

        public MethodWait<TInput, TOutput> WhenCancel(Action cancelAction)
        {
            MethodWaitEnitity.WhenCancel(cancelAction);
            return this;
        }

        public MethodWait<TInput, TOutput> MatchAny()
        {
            MethodWaitEnitity.MatchAny();
            return this;
        }

        public MethodWait<TInput, TOutput> NothingAfterMatch()
        {
            MethodWaitEnitity.NothingAfterMatch();
            return this;
        }

    }
}
using Hangfire.Annotations;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IBackgroundProcess
    {
        public string Enqueue([InstantHandle][NotNull] Expression<Func<Task>> methodCall);
        bool Delete([NotNull] string jobId);
        string Schedule([NotNull][InstantHandle] Expression<Func<Task>> methodCall, TimeSpan delay);
        string Schedule([NotNull][InstantHandle] Expression<Action> methodCall, TimeSpan delay);
        void AddOrUpdateRecurringJob<TClass>(
            [NotNull] string recurringJobId,
            [InstantHandle][NotNull] Expression<Func<TClass, Task>> methodCall,
            [NotNull] string cronExpression);
    }
}
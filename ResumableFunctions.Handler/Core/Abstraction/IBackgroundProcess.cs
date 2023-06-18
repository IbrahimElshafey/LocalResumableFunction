using System.Linq.Expressions;
using Hangfire.Annotations;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IBackgroundProcess
    {
        public string Enqueue([InstantHandle][NotNull] Expression<Func<Task>> methodCall);
        bool Delete([NotNull] string jobId);
        string Schedule([NotNull][InstantHandle] Expression<Func<Task>> methodCall, TimeSpan delay);
        string Schedule([NotNull][InstantHandle] Expression<Action> methodCall, TimeSpan delay);
    }
}
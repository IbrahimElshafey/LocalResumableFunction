using System.Linq.Expressions;
using Hangfire;
using Hangfire.Annotations;
using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Core;
internal class HangfireBackgroundProcess : IBackgroundProcess
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireBackgroundProcess(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void AddOrUpdateRecurringJob<TClass>(
        [NotNull] string recurringJobId, 
        [InstantHandle][NotNull] Expression<Func<TClass, Task>> methodCall,
        [NotNull] string cronExpression)
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
    }

    public bool Delete([NotNull] string jobId)
    {
        return _backgroundJobClient.Delete(jobId);
    }

    public string Enqueue([InstantHandle, NotNull] Expression<Func<Task>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    public string Schedule([InstantHandle, NotNull] Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    public string Schedule([InstantHandle, NotNull] Expression<Action> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }
}

using ResumableFunctions.Handler.Core.Abstraction;
using Hangfire.Annotations;
using System.Linq.Expressions;
using Hangfire;

namespace ResumableFunctions.Handler.Core;
internal class HangfireBackgroundProcess : IBackgroundProcess
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireBackgroundProcess(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
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

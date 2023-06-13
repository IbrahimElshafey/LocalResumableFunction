using ResumableFunctions.Handler.Core.Abstraction;
using Hangfire.Annotations;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace ResumableFunctions.Handler.Core;

internal class NoBackgroundProcess : IBackgroundProcess
{
    public bool Delete([NotNull] string jobId)
    {
        return true;
    }

    public string Enqueue([InstantHandle, NotNull] Expression<Func<Task>> methodCall)
    {
        var compiled = methodCall.CompileFast();
        compiled.Invoke().Wait();
        return default;
    }

    public string Schedule([InstantHandle, NotNull] Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke().Wait());
        return default;
    }

    public string Schedule([InstantHandle, NotNull] Expression<Action> methodCall, TimeSpan delay)
    {
        Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke());
        return default;
    }
}

using System.Linq.Expressions;
using FastExpressionCompiler;
using Hangfire.Annotations;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers.Expressions;

namespace ResumableFunctions.Handler.Core;

internal class NoBackgroundProcess : IBackgroundProcess
{
    public bool Delete([NotNull] string jobId)
    {
        return true;
    }

    public string Enqueue([InstantHandle, NotNull] Expression<Func<Task>> methodCall)
    {
        //methodCall = (Expression<Func<Task>>)TranslateConstants(methodCall);
        var compiled = methodCall.CompileFast();
        compiled.Invoke().Wait();
        return default;
    }

    public string Schedule([InstantHandle, NotNull] Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        //methodCall = (Expression<Func<Task>>)TranslateConstants(methodCall);
        Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke().Wait());
        return default;
    }

    public string Schedule([InstantHandle, NotNull] Expression<Action> methodCall, TimeSpan delay)
    {
        //methodCall = (Expression<Action>)TranslateConstants(methodCall);
        Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke());
        return default;
    }

    //private Expression TranslateConstants(Expression methodCall)
    //{
    //    var translateConstVisitor = new GenericVisitor();
    //    translateConstVisitor.OnVisitMember(me =>
    //    {
    //        var value = Expression.Lambda(me).CompileFast().DynamicInvoke();
    //        return Expression.Constant(value);
    //    });
    //    return translateConstVisitor.Visit(methodCall);
    //}
}

using System.Linq.Expressions;
using FastExpressionCompiler;
using Hangfire.Annotations;
using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Core;

internal class NoBackgroundProcess : IBackgroundProcess
{
    public bool Delete([NotNull] string jobId)
    {
        return true;
    }

    public string Enqueue([InstantHandle, NotNull] Expression<Func<Task>> methodCall)
    {
        try
        {
            var compiled = methodCall.CompileFast();
            compiled.Invoke().Wait();
            return default;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }
    }

    public string Schedule([InstantHandle, NotNull] Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        try
        {
            Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke().Wait());
            return default;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }
    }

    public string Schedule([InstantHandle, NotNull] Expression<Action> methodCall, TimeSpan delay)
    {
        try
        {
            Task.Delay(delay).ContinueWith(x => methodCall.CompileFast().Invoke());
            return default;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return default;
        }
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

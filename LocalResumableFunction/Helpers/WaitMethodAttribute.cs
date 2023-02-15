using LocalResumableFunction.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using System.Diagnostics;

namespace LocalResumableFunction.Helpers;

/// <summary>
///     Add this to the method you want to wait to.
/// </summary>
public sealed class WaitMethodAttribute : OnMethodBoundaryAspect
{
    private PushedMethod? _pushedMethod;
    public override object TypeId => nameof(WaitMethodAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        Debugger.Launch();
        args.MethodExecutionTag = false;
        _pushedMethod = new PushedMethod
        {
            MethodInfo = args.Method,
            Input = args.Arguments
        };
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        Debugger.Launch();
        _pushedMethod.Output = args.ReturnValue;
        _pushedMethod.Instance = args.Instance;
        //todo: main method must wait untill this completes
        _ = new ResumableFunctionHandler().MethodCalled(_pushedMethod);
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}
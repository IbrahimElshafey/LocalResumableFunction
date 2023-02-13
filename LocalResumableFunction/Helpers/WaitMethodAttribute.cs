using LocalResumableFunction.InOuts;
using MethodBoundaryAspect.Fody.Attributes;

namespace LocalResumableFunction.Helpers;

/// <summary>
///     Add this to the method you want to wait to.
/// </summary>
public sealed class WaitMethodAttribute : OnMethodBoundaryAspect
{
    private PushedMethod? _event;
    public override object TypeId => "WaitMethodAttribute";

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _event = new PushedMethod
        {
            CallerMethodInfo = args.Method,
            Input = args.Arguments
        };
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        _event.Output = args.ReturnValue;
        _event.Instance = (ResumableFunctionLocal)args.Instance;
        //todo: main method must wait untill this completes
        _ = new ResumableFunctionHandler().MethodCalled(_event);
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}
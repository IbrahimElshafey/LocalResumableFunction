using MethodBoundaryAspect.Fody.Attributes;

public sealed class ResumableMethodStartAttribute : OnMethodBoundaryAspect
{

}
public sealed class EventMethodAttribute : OnMethodBoundaryAspect
{
    private PushedEvent? _event;
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _event = new PushedEvent
        {
            CallerInfo = args.Method,
            Instance = args.Instance,
            Input = args.Arguments
        };
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        _event.Output = args.ReturnValue;
        _event.Instance = args.Instance;
        //todo: main method must wait untill this completes
        _ = ResumableFunctionLocal.WhenMethodCalled(_event);
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}

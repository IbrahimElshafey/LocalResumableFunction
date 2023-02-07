using LocalResumableFunction.InOuts;
using MethodBoundaryAspect.Fody.Attributes;

namespace LocalResumableFunction.Helpers
{
    public sealed class EventMethodAttribute : OnMethodBoundaryAspect
    {
        private PushCalledMethod? _event;
        public override void OnEntry(MethodExecutionArgs args)
        {
            args.MethodExecutionTag = false;
            _event = new PushCalledMethod
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
            _ = ResumableFunctionLocal.EventReceived(_event);
            args.MethodExecutionTag = true;
        }

        public override void OnException(MethodExecutionArgs args)
        {
            if ((bool)args.MethodExecutionTag)
                return;
            Console.WriteLine("On exception");
        }
    }
}
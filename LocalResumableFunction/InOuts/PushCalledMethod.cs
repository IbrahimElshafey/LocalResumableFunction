using System.Reflection;

namespace LocalResumableFunction.InOuts
{
    public class PushCalledMethod
    {
        public MethodBase CallerMethodInfo { get; internal set; }
        public ResumableFunctionLocal Instance { get; internal set; }
        public object[] Input { get; internal set; }
        public object Output { get; internal set; }
    }
}
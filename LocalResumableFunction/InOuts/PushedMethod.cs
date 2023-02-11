using System.Reflection;

namespace LocalResumableFunction.InOuts
{
    public class PushedMethod
    {
        public int MethodIdentifierId { get; internal set; }
        public MethodBase CallerMethodInfo { get; internal set; }
        public ResumableFunctionLocal Instance { get; internal set; }
        public object[] Input { get; internal set; }
        public object Output { get; internal set; }
    }
}
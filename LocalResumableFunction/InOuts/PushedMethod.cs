using System.Reflection;

namespace LocalResumableFunction.InOuts;

public class PushedMethod
{
    public MethodData MethodData { get; internal set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }
}
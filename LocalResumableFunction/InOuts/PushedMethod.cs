using System.Reflection;

namespace LocalResumableFunction.InOuts;

public class PushedMethod
{
    public MethodIdentifier MethodIdentifier { get; internal set; }
    public MethodBase MethodInfo { get; internal set; }
    public object Instance { get; internal set; }
    public object[] Input { get; internal set; }
    public object Output { get; internal set; }
}
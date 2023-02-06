using System.Reflection;

public class PushedEvent
{
    public MethodBase CallerInfo { get; internal set; }
    public object Instance { get; internal set; }
    public object[] Input { get; internal set; }
    public object Output { get; internal set; }
}

namespace ResumableFunctions.Handler.InOuts;

public class ExternalCallArgs
{
    public string MethodIdentifier { get; set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }
}
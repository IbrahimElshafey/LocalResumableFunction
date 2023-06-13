namespace ResumableFunctions.Handler.InOuts;

public class ExternalCallArgs
{
    public string ServiceName { get; set; }
    public string MethodUrn { get; set; }
    public dynamic Input { get; set; }
    public dynamic Output { get; set; }
}
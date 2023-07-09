namespace ResumableFunctions.Handler.InOuts;

public class AffectedService
{
    public int PushedCallId { get; set; }
    public string MethodUrn { get; set; }
    public int MethodGroupId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; }
    public string ServiceUrl { get; set; }  
    public List<int> AffectedFunctionsIds { get; set; }
}

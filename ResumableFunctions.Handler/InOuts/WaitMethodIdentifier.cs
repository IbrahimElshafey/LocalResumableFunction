namespace ResumableFunctions.Handler.InOuts;

public  class WaitMethodIdentifier : MethodIdentifier
{
   
    public WaitMethodGroup WaitMethodGroup { get; internal set; }
    public int WaitMethodGroupId { get; internal set; }
}

public class WaitMethodGroup
{
    public int Id { get; internal set; }
    public string MethodGroupUrn { get; internal set; }
    public List<WaitMethodIdentifier> WaitMethodIdentifiers { get; internal set; } = new();
    public List<MethodWait> WaitsRequestsForMethod { get; internal set; }

}


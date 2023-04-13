namespace ResumableFunctions.Handler.InOuts;

public  class WaitMethodIdentifier : MethodIdentifier
{
   
    public MethodsGroup ParentMethodGroup { get; internal set; }
    public int ParentMethodGroupId { get; internal set; }

    public List<MethodWait> WaitsRequestsForMethod { get; internal set; }
}


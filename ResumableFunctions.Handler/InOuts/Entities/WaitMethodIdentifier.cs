namespace ResumableFunctions.Handler.InOuts.Entities;

public class WaitMethodIdentifier : MethodIdentifier
{
    public MethodsGroup MethodGroup { get; set; }
    public int MethodGroupId { get; set; }
    //public List<MethodWaitEntity> Waits { get; internal set; }
}


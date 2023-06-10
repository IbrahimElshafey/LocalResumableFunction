namespace ResumableFunctions.Handler.InOuts;

public  class WaitMethodIdentifier : MethodIdentifier
{
    public bool CanPublishFromExternal { get; internal set; }

    public MethodsGroup MethodGroup { get; internal set; }
    public int MethodGroupId { get; internal set; }

    internal override void FillFromMethodData(MethodData methodData)
    {
        base.FillFromMethodData(methodData);
        CanPublishFromExternal = methodData.CanPublishFromExternal;
    }
}


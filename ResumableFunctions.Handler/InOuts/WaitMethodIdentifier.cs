namespace ResumableFunctions.Handler.InOuts;

public  class WaitMethodIdentifier : MethodIdentifier
{
    public bool CanPublishFromExternal { get; internal set; }

    public MethodsGroup ParentMethodGroup { get; internal set; }
    public int ParentMethodGroupId { get; internal set; }

    internal override void FillFromMethodData(MethodData methodData)
    {
        base.FillFromMethodData(methodData);
        CanPublishFromExternal = methodData.CanPublishFromExternal;
    }
}


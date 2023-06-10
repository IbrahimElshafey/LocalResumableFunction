namespace ResumableFunctions.Handler.InOuts;

public class MethodsGroup : IEntity, IEntityInService
{
    public int Id { get; internal set; }
    public string MethodGroupUrn { get; internal set; }
    public List<WaitMethodIdentifier> WaitMethodIdentifiers { get; internal set; } = new();
    public List<MethodWait> WaitRequestsForGroup { get; internal set; }

    public DateTime Created { get; internal set; }
    public int? ServiceId { get; set; }
    public List<MethodWaitTemplate> WaitTemplates { get; internal set; }
}


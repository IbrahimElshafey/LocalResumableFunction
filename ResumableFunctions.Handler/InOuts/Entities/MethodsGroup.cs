namespace ResumableFunctions.Handler.InOuts.Entities;

public class MethodsGroup : IEntity<int>
{
    internal MethodsGroup()
    {

    }
    public int Id { get; set; }
    public string MethodGroupUrn { get; set; }
    public bool CanPublishFromExternal { get; set; }
    public bool IsLocalOnly { get; set; }
    public List<WaitMethodIdentifier> WaitMethodIdentifiers { get; set; } = new();
    public List<MethodWaitEntity> WaitRequestsForGroup { get; set; }

    public DateTime Created { get; set; }
    public int? ServiceId { get; set; }
    public List<WaitTemplate> WaitTemplates { get; set; }
}


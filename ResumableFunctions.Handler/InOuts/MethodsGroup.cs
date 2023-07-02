﻿namespace ResumableFunctions.Handler.InOuts;

public class MethodsGroup : IEntity
{
    public long Id { get; internal set; }
    public string MethodGroupUrn { get; internal set; }
    public List<WaitMethodIdentifier> WaitMethodIdentifiers { get; internal set; } = new();
    public List<MethodWait> WaitRequestsForGroup { get; internal set; }

    public DateTime Created { get; internal set; }
    public long? ServiceId { get; set; }
    public List<WaitTemplate> WaitTemplates { get; internal set; }
}


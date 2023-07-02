namespace ResumableFunctions.Handler.InOuts;

public class ScanState : IEntity
{
    public long Id { get; internal set; }

    public DateTime Created { get; internal set; }

    public long? ServiceId { get; internal set; }

    public string Name { get; set; }
}

namespace ResumableFunctions.Handler.InOuts;

public class ScanState : IEntity
{
    public int Id { get; internal set; }

    public DateTime Created { get; internal set; }

    public int? ServiceId { get; internal set; }

    public string Name { get; set; }
}

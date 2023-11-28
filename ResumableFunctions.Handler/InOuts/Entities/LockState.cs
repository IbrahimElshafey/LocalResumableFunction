namespace ResumableFunctions.Handler.InOuts.Entities;
public class LockState : IEntity<int>
{
    public int Id { get; set; }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public string Name { get; set; }
    public string ServiceName { get; set; }
}

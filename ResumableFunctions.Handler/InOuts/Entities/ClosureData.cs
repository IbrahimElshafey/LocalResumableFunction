namespace ResumableFunctions.Handler.InOuts.Entities;

public class ClosureData : IEntity<Guid>, IEntityWithUpdate
{
    public Guid Id { get; set; }
    public long RootId { get; set; }
    public string CallerName { get; set; }
    public object Closure { get; set; }
    //public List<WaitEntity> LinkedWaits { get; set; }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }
}

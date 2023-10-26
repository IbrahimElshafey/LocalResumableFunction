namespace ResumableFunctions.Handler.InOuts.Entities;

public class ClosureData : IEntity<long>,IEntityWithUpdate
{
    /// <summary>
    /// RootWaitId
    /// </summary>
    public long Id { get; set; }
    public object Locals { get; set; }
    public object Closure { get; set; }

    internal void SetLocalsAsType(Type type) { }
    internal void SetClosureAsType(Type type) { }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }
}

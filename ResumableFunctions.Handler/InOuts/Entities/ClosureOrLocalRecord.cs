namespace ResumableFunctions.Handler.InOuts.Entities;

public class ClosureData : IEntity<long>,IEntityWithUpdate
{
    public int RootWaitId { get; set; }

    public string LocalValue { get; set; }
    public string ClosureValue { get; set; }

    public long Id { get; set; }//PK

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }
}

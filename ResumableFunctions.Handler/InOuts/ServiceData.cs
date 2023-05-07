namespace ResumableFunctions.Handler.InOuts;
public class ServiceData : EntityWithLogs, IEntityWithUpdate
{
    public string AssemblyName { get; internal set; }
    public string Url { get; internal set; }
    public DateTime Modified { get; internal set; }
    public int ParentId { get; internal set; }
    public string ConcurrencyToken { get; internal set; }
}

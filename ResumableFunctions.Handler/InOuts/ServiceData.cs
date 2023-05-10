namespace ResumableFunctions.Handler.InOuts;
public class ServiceData : ObjectWithLog, IEntityWithUpdate
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public string AssemblyName { get; internal set; }
    public string Url { get; internal set; }
    public DateTime Modified { get; internal set; }
    public int ParentId { get; internal set; }
    public string ConcurrencyToken { get; internal set; }
}

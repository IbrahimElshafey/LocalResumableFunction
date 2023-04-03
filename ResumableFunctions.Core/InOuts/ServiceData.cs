namespace ResumableFunctions.Core.InOuts;
public class ServiceData
{
    public int Id { get; internal set; }
    public string AssemblyName { get; internal set; }
    public string Url { get; internal set; }
    public DateTime LastScanDate { get; internal set; }
    public int ParentId { get; internal set; }
}

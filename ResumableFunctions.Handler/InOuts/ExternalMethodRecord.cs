
namespace ResumableFunctions.Handler.InOuts;

public class ExternalMethodRecord
{
    public int Id { get; internal set; }
    public MethodData MethodData { get; set; }

    public byte[] MethodHash { get; internal set; }

    public byte[] OriginalMethodHash { get; internal set; }
    public string TrackingId { get; internal set; }

    //todo:to use
    public bool IsOriginalMethodExist { get; set; }
    public bool IsWebhook { get; set; }
}

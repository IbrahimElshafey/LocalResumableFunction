namespace ResumableFunctions.Core.InOuts;

public class WebhookCall
{
    public string WebhookIdentifier { get; set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }
}

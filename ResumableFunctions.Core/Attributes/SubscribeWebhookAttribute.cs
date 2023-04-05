namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class SubscribeWebhookAttribute : Attribute
{
    public SubscribeWebhookAttribute(string webhookIdetifier)
    {
        if (string.IsNullOrWhiteSpace(webhookIdetifier))
            throw new ArgumentNullException("WebhookIdentifier can't be null or empty.");
        WebhookIdentifier = webhookIdetifier;
    }
    public string WebhookIdentifier { get; }
}

namespace ResumableFunctions.Publisher
{
    public interface IPublisherSettings
    {
        string ConsumerServiceUrl { get;}
        Type PublishCallImplementation { get; }
    }
}

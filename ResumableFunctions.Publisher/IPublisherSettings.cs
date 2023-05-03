namespace ResumableFunctions.Publisher
{
    public interface IPublisherSettings
    {
        Dictionary<string,string> ServicesRegistry { get;}
        Type PublishCallImplementation { get; }
    }
}

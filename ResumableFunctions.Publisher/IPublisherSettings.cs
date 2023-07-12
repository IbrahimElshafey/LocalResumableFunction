namespace ResumableFunctions.Publisher
{
    public interface IPublisherSettings
    {
        Dictionary<string,string> ServicesRegistry { get;}
        Type CallPublisherType { get; }
        TimeSpan CheckFailedRequestEvery { get; }
    }
}

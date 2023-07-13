namespace ResumableFunctions.Publisher.Abstraction
{
    public interface IPublisherSettings
    {
        Dictionary<string,string> ServicesRegistry { get;}
        Type CallPublisherType { get; }
        TimeSpan CheckFailedRequestEvery { get; }
    }
}

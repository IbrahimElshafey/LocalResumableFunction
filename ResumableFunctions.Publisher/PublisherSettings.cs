namespace ResumableFunctions.Publisher
{
    public class PublisherSettings: IPublisherSettings
    {
        public string ConsumerServiceUrl { get;}

        public PublisherSettings(string consumerServiceUrl)
        {
            ConsumerServiceUrl = consumerServiceUrl;
        }


        public Type PublishCallImplementation => typeof(PublishCallDirect);
    }
}

namespace ResumableFunctions.Publisher
{
    public class PublisherSettings : IPublisherSettings
    {

        public PublisherSettings(Dictionary<string, string> servicesRegistry)
        {
            ServicesRegistry = servicesRegistry;
        }


        public Type CallPublisherType => typeof(HttpCallPublisher);

        public Dictionary<string, string> ServicesRegistry { get; }
    }
}

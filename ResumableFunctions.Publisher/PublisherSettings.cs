namespace ResumableFunctions.Publisher
{
    public class PublisherSettings : IPublisherSettings
    {

        public PublisherSettings(Dictionary<string, string> servicesRegistry, TimeSpan checkFailedRequestEvery = default)
        {
            ServicesRegistry = servicesRegistry;
            if (checkFailedRequestEvery != default)
                _checkFailedRequestEvery = checkFailedRequestEvery;
        }


        public Type CallPublisherType => typeof(HttpCallPublisher);

        public Dictionary<string, string> ServicesRegistry { get; }

        private TimeSpan _checkFailedRequestEvery = TimeSpan.FromMinutes(30);
        public TimeSpan CheckFailedRequestEvery => _checkFailedRequestEvery;
    }
}

using LiteDB;

namespace ResumableFunctions.Publisher
{
    internal class FailedRequestHandler : IFailedRequestHandler
    {
        private readonly IPublisherSettings _settings;

        public FailedRequestHandler(IPublisherSettings settings)
        {
            _settings = settings;
        }

        public void AddFailedRequest(FailedRequest failedRequest)
        {
            using (var db = new LiteDatabase(Constants.FailedRequestsDb))
            {
                var failedRequests = db.GetCollection<FailedRequest>(Constants.FailedRequestsCollection);
                failedRequest.Created = DateTime.Now;
                failedRequests.Insert(failedRequest);
                //db.Commit();
            }
        }
    }
}

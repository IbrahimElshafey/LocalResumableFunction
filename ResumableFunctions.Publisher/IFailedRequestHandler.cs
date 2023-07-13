namespace ResumableFunctions.Publisher
{
    public interface IFailedRequestHandler
    {
        void AddFailedRequest(FailedRequest failedRequest);
        void HandleFailedRequests();
    }
}

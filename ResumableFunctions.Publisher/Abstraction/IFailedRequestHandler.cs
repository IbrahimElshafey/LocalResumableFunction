using ResumableFunctions.Publisher.InOuts;

namespace ResumableFunctions.Publisher.Abstraction
{
    public interface IFailedRequestHandler
    {
        void AddFailedRequest(FailedRequest failedRequest);
        void HandleFailedRequests();
    }
}

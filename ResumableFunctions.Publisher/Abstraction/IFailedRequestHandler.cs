using ResumableFunctions.Publisher.InOuts;
using System.Threading.Tasks;

namespace ResumableFunctions.Publisher.Abstraction
{
    public interface IFailedRequestHandler
    {
        void AddFailedRequest(FailedRequest failedRequest);
        Task HandleFailedRequests();
    }
}

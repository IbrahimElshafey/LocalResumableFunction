using ResumableFunctions.Publisher.Abstraction;
using ResumableFunctions.Publisher.InOuts;
using System;
using System.Threading.Tasks;
namespace ResumableFunctions.Publisher.Implementation
{
    /// <summary>
    /// https://microsoft.github.io/FASTER/docs/fasterlog-basics/
    /// https://microsoft.github.io/FASTER/docs/fasterkv-basics/
    /// </summary>
    internal class FasterDbFailedRequestHandler : IFailedRequestHandler
    {
        public Task EnqueueFailedRequest(FailedRequest failedRequest)
        {
            throw new NotImplementedException();
        }

        public Task HandleFailedRequests()
        {
            throw new NotImplementedException();
        }
    }
}

using ResumableFunctions.Publisher.Abstraction;
using ResumableFunctions.Publisher.InOuts;
using System;
using System.Threading.Tasks;
namespace ResumableFunctions.Publisher.Implementation
{
    /// <summary>
    /// https://microsoft.github.io/FASTER/docs/fasterkv-basics/
    /// </summary>
    internal class FasterDbFailedRequestHandler : IFailedRequestHandler
    {
        public void AddFailedRequest(FailedRequest failedRequest)
        {
            throw new NotImplementedException();
        }

        public Task HandleFailedRequests()
        {
            throw new NotImplementedException();
        }
    }
}

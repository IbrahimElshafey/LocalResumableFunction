using ResumableFunctions.Publisher.InOuts;
using System;
using System.Threading.Tasks;

namespace ResumableFunctions.Publisher.Abstraction
{
    public interface ICallPublisher
    {
        Task Publish<TInput, TOutput>(
            Func<TInput, Task<TOutput>> methodToPush,
            TInput input,
            TOutput output,
            string methodUrn,
            string serviceName);
        Task Publish(MethodCall MethodCall);
    }
}

using ResumableFunctions.Publisher.InOuts;

namespace ResumableFunctions.Publisher
{
    public interface ICallPublisher
    {
        Task Publish<TInput, TOutput>(
            Func<TInput, Task<TOutput>> methodToPush,
            TInput input,
            TOutput output,
            string methodIdetifier,
            string serviceName);
        Task Publish(MethodCall MethodCall);
    }
}

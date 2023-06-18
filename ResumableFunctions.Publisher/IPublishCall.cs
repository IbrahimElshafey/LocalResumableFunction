namespace ResumableFunctions.Publisher
{
    public interface IPublishCall
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

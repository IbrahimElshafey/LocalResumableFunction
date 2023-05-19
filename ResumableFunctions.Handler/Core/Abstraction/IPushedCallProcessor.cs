using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IPushedCallProcessor
    {
        Task ProcessPushedCall(int pushedCallId);
        Task<int> QueuePushedCallProcessing(PushedCall pushedCall);
        Task<int> QueueExternalPushedCallProcessing(PushedCall pushedCall, string serviceName);
    }
}
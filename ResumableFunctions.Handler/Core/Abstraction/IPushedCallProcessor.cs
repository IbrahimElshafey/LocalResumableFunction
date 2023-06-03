using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IPushedCallProcessor
    {
        Task InitialProcessPushedCall(int pushedCallId,string methodUrn);
        Task<int> QueuePushedCallProcessing(PushedCall pushedCall);
        Task<int> QueueExternalPushedCallProcessing(PushedCall pushedCall, string serviceName);
        Task ServiceProcessPushedCall(int pushedCallId, string methodUrn);
    }
}
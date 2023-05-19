using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler
{
    public interface IPushedCallProcessor
    {
        Task ProcessPushedCall(int pushedCallId);
        Task<int> QueuePushedCallProcessing(PushedCall pushedCall);
    }
}
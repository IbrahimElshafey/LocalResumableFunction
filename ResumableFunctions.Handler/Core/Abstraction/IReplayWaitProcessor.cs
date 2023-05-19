using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler
{
    internal interface IReplayWaitProcessor
    {
        Task<(Wait Wait, bool ProceedExecution)> ReplayWait(ReplayRequest replayRequest);
    }
}
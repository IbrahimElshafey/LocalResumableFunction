using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    internal interface IReplayWaitProcessor
    {
        Task<(Wait Wait, bool ProceedExecution)> ReplayWait(ReplayRequest replayRequest);
    }
}
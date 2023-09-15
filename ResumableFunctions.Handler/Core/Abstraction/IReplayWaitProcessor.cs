using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    internal interface IReplayWaitProcessor
    {
        Task<(WaitEntity Wait, bool ProceedExecution)> GetWaitToReplay(ReplayRequest replayRequest);
    }
}
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitProcessingRecordsRepo
{
    WaitProcessingRecord Add(WaitProcessingRecord waitProcessingRecord);
    Task<List<WaitProcessingRecord>> GetWaitsForCall(int pushedCallId, int functionId);
}
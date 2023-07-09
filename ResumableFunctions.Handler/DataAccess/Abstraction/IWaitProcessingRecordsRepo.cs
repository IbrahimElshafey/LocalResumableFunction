using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitProcessingRecordsRepo
{
    Task<WaitProcessingRecord> Add(WaitProcessingRecord waitProcessingRecord);
}
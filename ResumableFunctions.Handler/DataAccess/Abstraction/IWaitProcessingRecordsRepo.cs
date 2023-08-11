using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitProcessingRecordsRepo
{
    WaitProcessingRecord Add(WaitProcessingRecord waitProcessingRecord);
}
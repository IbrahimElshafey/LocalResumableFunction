using ResumableFunctions.Data.Abstraction.Entities;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IWaitProcessingRecordsRepo
    {
        WaitProcessingRecord Add(WaitProcessingRecord waitProcessingRecord);
    }
}
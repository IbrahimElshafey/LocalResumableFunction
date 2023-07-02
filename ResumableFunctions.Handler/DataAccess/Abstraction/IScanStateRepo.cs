namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IScanStateRepo
    {
        Task<long> AddScanState(string name);
        Task<bool> RemoveScanState(long id);
        Task<bool> IsScanFinished();
    }
}
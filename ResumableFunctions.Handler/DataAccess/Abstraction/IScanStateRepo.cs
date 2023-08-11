namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IScanStateRepo
    {
        Task<int> AddScanState(string name);
        Task<bool> RemoveScanState(int id);
        Task<bool> IsScanFinished();

        Task ResetServiceScanState();
    }
}
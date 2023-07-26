using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

//todo:it should be transient
internal class ScanStateRepo : IScanStateRepo
{
    private readonly WaitsDataContext _context;
    private readonly IDistributedLockProvider _lockProvider;

    public ScanStateRepo(WaitsDataContext context, IDistributedLockProvider lockProvider)
    {
        _context = context;
        _lockProvider = lockProvider;
    }
    public async Task<bool> IsScanFinished()
    {
        await using var lockScanStat = await _lockProvider.AcquireLockAsync("ScanStateLock");
        return await _context.ScanStates.AnyAsync() is false;
    }

    public async Task<int> AddScanState(string name)
    {
        var toAdd = new ScanState { Name = name };
        _context.ScanStates.Add(toAdd);
        await _context.SaveChangesAsync();
        return toAdd.Id;
    }

    public async Task<bool> RemoveScanState(int id)
    {
        await using var lockScanStat = await _lockProvider.AcquireLockAsync("ScanStateLock");
        await _context.ScanStates.Where(x => x.Id == id).ExecuteDeleteAsync();
        return true;
    }
}

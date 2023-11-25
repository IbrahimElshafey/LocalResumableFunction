using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess;

internal class ScanStateRepo : IScanStateRepo
{
    private readonly string _scanStateLockName;
    private readonly WaitsDataContext _context;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IResumableFunctionsSettings _settings;

    public ScanStateRepo(
        WaitsDataContext context,
        IDistributedLockProvider lockProvider,
        IResumableFunctionsSettings settings)
    {
        _context = context;
        _lockProvider = lockProvider;
        _settings = settings;
        //should not contain ServiceName
        //_scanStateLockName = $"{_settings.CurrentWaitsDbName}_{_settings.CurrentServiceName}_ScanStateLock";
        _scanStateLockName = $"{_settings.CurrentWaitsDbName}_ScanStateLock";
    }
    public async Task<bool> IsScanFinished()
    {
        await using var lockScanStat = await _lockProvider.AcquireLockAsync(_scanStateLockName);
        return await _context.ScanStates.AnyAsync() is false;
    }

    public async Task<int> AddScanState(string name)
    {
        var toAdd = new ScanState { Name = name, ServiceName = _settings.CurrentServiceName };
        _context.ScanStates.Add(toAdd);
        await _context.SaveChangesAsync();
        return toAdd.Id;
    }

    public async Task<bool> RemoveScanState(int id)
    {
        if (id == -1) return true;
        await using var lockScanStat = await _lockProvider.AcquireLockAsync(_scanStateLockName);
        await _context.ScanStates.Where(x => x.Id == id).ExecuteDeleteAsync();
        return true;
    }

    public async Task ResetServiceScanState()
    {
        await using var lockScanStat = await _lockProvider.AcquireLockAsync(_scanStateLockName);
        await _context.ScanStates.Where(x => x.ServiceName == _settings.CurrentServiceName).ExecuteDeleteAsync();
    }
}

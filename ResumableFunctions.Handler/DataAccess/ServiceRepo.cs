using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;
using System.Reflection;
using System.Runtime;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.DataAccess;

internal class ServiceRepo : IServiceRepo
{
    private readonly WaitsDataContext _context;
    private readonly IResumableFunctionsSettings _settings;
    private readonly ILogger<ServiceRepo> _logger;

    public ServiceRepo(
        WaitsDataContext context,
        IResumableFunctionsSettings settings,
        ILogger<ServiceRepo> logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public async Task UpdateDllScanDate(ServiceData dll)
    {
        await _context.Entry(dll).ReloadAsync();
        dll.AddLog($"Update last scan date for service [{dll.AssemblyName}] to [{DateTime.Now}].");
        dll.Modified = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOldScanData(DateTime dateBeforeScan)
    {
        await _context
            .Logs
            .Where(x =>
                x.EntityId == _settings.CurrentServiceId &&
                x.EntityType == nameof(ServiceData) &&
                x.Created < dateBeforeScan)
            .ExecuteDeleteAsync();
    }

    public async Task<bool> ShouldScanAssembly(string assemblyPath)
    {
        var currentAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var serviceData = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName) ??
                          await AddNewServiceData(currentAssemblyName);

        var notRoot = serviceData.Id != _settings.CurrentServiceId;
        var notInCurrent = serviceData.ParentId != _settings.CurrentServiceId;
        if (notInCurrent && notRoot)
        {
            var rootService = _context.ServicesData.Local.FirstOrDefault(x => x.Id == _settings.CurrentServiceId);
            rootService?.AddError($"Dll `{currentAssemblyName}` will not be added to service `{Assembly.GetEntryAssembly()?.GetName().Name}` because it's used in another service.", null, ErrorCodes.Scan);
            return false;
        }

        _settings.CurrentServiceId = serviceData.ParentId == -1 ? serviceData.Id : serviceData.ParentId;
        if (File.Exists(assemblyPath) is false)
        {
            var message = $"Assembly file ({assemblyPath}) not exist.";
            _logger.LogError(message);
            serviceData.AddError(message, null, ErrorCodes.Scan);
            return false;
        }

        serviceData.ErrorCounter = 0;

        if (serviceData.ParentId == -1)
        {
            await _context
               .ServicesData
               .Where(x => x.ParentId == serviceData.Id)
               .ExecuteDeleteAsync();
        }


        var assembly = Assembly.LoadFile(assemblyPath);
        var isReferenceResumableFunction =
            assembly.GetReferencedAssemblies().Any(x => new[]
            {
                "ResumableFunctions.Handler",
                "ResumableFunctions.AspNetService"
            }.Contains(x.Name));

        if (isReferenceResumableFunction is false)
        {
            serviceData.AddError($"No reference for ResumableFunction DLLs found,The scan canceled for [{assemblyPath}].", null, ErrorCodes.Scan);
            return false;
        }

        var lastBuildDate = File.GetLastWriteTime(assemblyPath);
        serviceData.Url = _settings.CurrentServiceUrl;
        serviceData.AddLog($"Check last scan date for assembly [{currentAssemblyName}].");
        var shouldScan = lastBuildDate > serviceData.Modified;
        if (shouldScan is false)
            serviceData.AddLog($"No need to rescan assembly [{currentAssemblyName}].");
        if (_settings.ForceRescan)
            serviceData.AddLog(
                $"Dll `{currentAssemblyName}` Will be scanned because force rescan is enabled.", LogType.Warning, ErrorCodes.Scan);
        return shouldScan || _settings.ForceRescan;
    }

    public async Task<ServiceData> GetServiceData(string assemblyName)
    {
        return await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == assemblyName);
    }

    public async Task AddErrorLog(Exception ex, string errorMsg, int errorCode = 0)
    {
        _logger.LogError(ex, errorMsg);
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = $"{errorMsg}\n{ex}",
            Type = LogType.Error,
            Code = errorCode
        });
        await _context.SaveChangesAsync();
    }

    private async Task<ServiceData> AddNewServiceData(string currentAssemblyName)
    {
        var parentId = _settings.CurrentServiceId;
        var newServiceData = new ServiceData
        {
            AssemblyName = currentAssemblyName,
            Url = _settings.CurrentServiceUrl,
            ParentId = parentId
        };
        _context.ServicesData.Add(newServiceData);
        newServiceData.AddLog($"Assembly [{currentAssemblyName}] will be scanned.");
        await _context.SaveChangesAsync();
        return newServiceData;
    }

    public async Task AddLog(string msg, LogType logType = LogType.Info, int errorCode = 0)
    {
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = msg,
            Type = logType,
            Code = errorCode
        });
        await _context.SaveChangesAsync();
    }
}
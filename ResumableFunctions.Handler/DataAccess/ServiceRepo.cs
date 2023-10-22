﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Reflection;

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
        dll.AddLog($"Update last scan date for service [{dll.AssemblyName}] to [{DateTime.Now}].", LogType.Info, StatusCodes.Scanning);
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

    public async Task<ServiceData> FindServiceDataForScan(string currentAssemblyName)
    {
        var serviceData =
            await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == currentAssemblyName) ??
            await AddNewServiceData(currentAssemblyName);

        var notRoot = serviceData.Id != _settings.CurrentServiceId;
        var notInCurrent = serviceData.ParentId != _settings.CurrentServiceId;
        if (notInCurrent && notRoot)
        {
            var rootService = _context.ServicesData.Local.FirstOrDefault(x => x.Id == _settings.CurrentServiceId);
            rootService?.AddError(
                $"Dll [{currentAssemblyName}] will not be added to service " +
                $"[{Assembly.GetEntryAssembly()?.GetName().Name}] because it's used in another service.",
                StatusCodes.Scanning, null);
            return null;
        }

        _settings.CurrentServiceId = serviceData.ParentId == -1 ? serviceData.Id : serviceData.ParentId;
        //delete dll related if parent service 
        if (serviceData.ParentId == -1)
        {
            await _context
               .ServicesData
               .Where(x => x.ParentId == serviceData.Id)
               .ExecuteDeleteAsync();
        }
        return serviceData;
    }

    public async Task<ServiceData> GetServiceData(string assemblyName)
    {
        return await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == assemblyName);
    }

    public async Task AddErrorLog(Exception ex, string errorMsg, int errorCode)
    {
        _logger.LogError(ex, errorMsg);
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = $"{errorMsg}\n{ex}",
            Type = LogType.Error,
            StatusCode = errorCode
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
        newServiceData.AddLog($"Assembly [{currentAssemblyName}] will be scanned.", LogType.Info, StatusCodes.Scanning);
        await _context.SaveChangesAsync();
        return newServiceData;
    }

    public async Task AddLog(string msg, LogType logType, int errorCode)
    {
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = msg,
            Type = logType,
            StatusCode = errorCode
        });
        await _context.SaveChangesAsync();
    }
}
﻿using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IServiceRepo
{
    Task UpdateDllScanDate(ServiceData dll);
    Task DeleteOldScanData(DateTime dateBeforeScan);
    Task<bool> ShouldScanAssembly(string assemblyPath);
    Task<ServiceData> GetServiceData(string assemblyName);

    Task AddErrorLog(Exception ex, string errorMsg, int errorCode = 0);
    Task AddLog(string msg, LogType logType = LogType.Info, int errorCode = 0);
}
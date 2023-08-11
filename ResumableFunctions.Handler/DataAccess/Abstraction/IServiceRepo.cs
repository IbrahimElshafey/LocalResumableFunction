using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IServiceRepo
{
    Task UpdateDllScanDate(ServiceData dll);
    Task DeleteOldScanData(DateTime dateBeforeScan);
    Task<bool> ShouldScanAssembly(string assemblyPath);//todo:this is not pure data function
    Task<ServiceData> GetServiceData(string assemblyName);

    Task AddErrorLog(Exception ex, string errorMsg, int errorCode);
    Task AddLog(string msg, LogType logType, int errorCode);
}
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;
public interface IServiceRepo
{
    Task UpdateDllScanDate(ServiceData dll);
    Task DeleteOldScanData(DateTime dateBeforeScan);
    Task<ServiceData> FindServiceDataForScan(string assemblyName);
    Task<ServiceData> GetServiceData(string assemblyName);
}
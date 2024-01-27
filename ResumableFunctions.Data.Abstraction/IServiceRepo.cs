using System.Threading.Tasks;
using System;
using ResumableFunctions.Data.Abstraction.Entities;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IServiceRepo
    {
        Task UpdateDllScanDate(ServiceData dll);
        Task DeleteOldScanData(DateTime dateBeforeScan);
        Task<ServiceData> FindServiceDataForScan(string assemblyName);
        Task<ServiceData> GetServiceData(string assemblyName);
    }
}
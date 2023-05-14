using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.UiService
{
    public interface IUiService
    {
        Task<MainStatistics> GetMainStatistics();
        Task<ServiceData> GetServiceInfo(int serviceId);
        Task<List<LogRecord>> GetServiceLogs(int serviceId);
        Task<List<ServiceInfo>> GetServicesList();
        Task<ServiceStatistics> GetServiceStatistics(int serviceId);
        Task<List<FunctionInfo>> GetFunctionsInfo(int? serviceId);
        Task<List<MethodGroupInfo>> GetMethodsInfo(int? serviceId);
    }
}

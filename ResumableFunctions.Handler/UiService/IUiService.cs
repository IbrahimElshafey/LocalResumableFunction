using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.Handler.UiService
{
    public interface IUiService
    {
        //Task<MainStatistics> GetMainStatistics();
        Task<List<ServiceData>> GetServices();
        Task<ServiceData> GetServiceInfo(int serviceId);
        Task<List<LogRecord>> GetServiceLogs(int serviceId);
        Task<List<LogRecord>> GetLogs(int page = 0);
        Task<List<ServiceInfo>> GetServicesList();
        Task<ServiceStatistics> GetServiceStatistics(int serviceId);
        Task<List<FunctionInfo>> GetFunctionsInfo(int? serviceId);
        Task<List<MethodGroupInfo>> GetMethodsInfo(int? serviceId);
        Task<List<PushedCallInfo>> GetPushedCalls(int page);
        Task<List<FunctionInstanceInfo>> GetFunctionInstances(int functionId);
        Task<PushedCallDetails> GetPushedCallDetails(int pushedCallId);
        Task<FunctionInstanceDetails> GetInstanceDetails(int instanceId);
        Task<List<MethodInGroupInfo>> GetMethodsInGroup(int groupId);
        Task<List<MethodWaitDetails>> GetWaitsForGroup(int groupId);
    }
}

using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.Handler.UiService
{
    public interface IUiService
    {
        //Task<MainStatistics> GetMainStatistics();
        Task<List<ServiceData>> GetServices();
        Task<ServiceData> GetServiceInfo(long serviceId);
        Task<List<LogRecord>> GetServiceLogs(long serviceId);
        Task<List<LogRecord>> GetLogs(int page = 0);
        Task<List<ServiceInfo>> GetServicesList();
        Task<ServiceStatistics> GetServiceStatistics(long serviceId);
        Task<List<FunctionInfo>> GetFunctionsInfo(long? serviceId);
        Task<List<MethodGroupInfo>> GetMethodsInfo(long? serviceId);
        Task<List<PushedCallInfo>> GetPushedCalls(long page);
        Task<List<FunctionInstanceInfo>> GetFunctionInstances(long functionId);
        Task<PushedCallDetails> GetPushedCallDetails(long pushedCallId);
        Task<FunctionInstanceDetails> GetInstanceDetails(long instanceId);
        Task<List<MethodInGroupInfo>> GetMethodsInGroup(long groupId);
        Task<List<MethodWaitDetails>> GetWaitsForGroup(long groupId);
    }
}

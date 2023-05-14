using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record MainStatistics(
        int Services,
        int ResumableFunctions,
        int ResumableFunctionsInstances,
        int Methods,
        int PushedCalls,
        int LatestLogErrors);

    public record ServiceInfo(int Id, string Name, string Url)
    {
        public int LogErrors { get; set; }
        public int FunctionsCount { get; set; }
        public int MethodsCount { get; set; }
    }

    public record ServiceStatistics(int Id, string ServiceName, int ErrorCounter, int FunctionsCount, int MethodsCount);
    public record FunctionInfo(
        int Id, string URN, string FullName,bool IsEntryPoint,string FirstWait, int InProgress, int Completed, int Failed, DateTime Created, DateTime LastUpdated);

    public record MethodGroupInfo(
        int Id, string URN, int MethodsCount,int ActiveWaits,int CompletedWaits,int CanceledWaits,DateTime Created);
}

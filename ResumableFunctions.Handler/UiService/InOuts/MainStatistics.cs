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

    public record ServiceInfo(int Id,string Name,string Url,int ScanLogErrors,int FunctionsCount,int MethodsCount);
}

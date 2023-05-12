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

        Task<List<ServiceInfo>> GetServicesInfo();
    }
}

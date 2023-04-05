using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core.InOuts
{
    public interface IResumableFunctionSettings
    {
        public IGlobalConfiguration HangFireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
        public string CurrentServiceUrl { get; }
        public string[] DllsToScan { get; set; }

        public bool ForceRescan { get; set; }
    }
}

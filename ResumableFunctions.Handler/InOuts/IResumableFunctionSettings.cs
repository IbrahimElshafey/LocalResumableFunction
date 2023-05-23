using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Handler.InOuts
{
    public interface IResumableFunctionsSettings
    {
        public IGlobalConfiguration HangFireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
        public string CurrentServiceUrl { get; }
        public string SyncServerConnection { get; }
        public string[] DllsToScan { get;  }

        public bool ForceRescan { get; set; }
    }
}

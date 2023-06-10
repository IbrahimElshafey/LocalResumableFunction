using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Handler.InOuts
{
    public interface IResumableFunctionsSettings
    {
        public IGlobalConfiguration HangfireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
        public string CurrentServiceUrl { get; }
        public int CurrentServiceId { get; internal set; }
        public string SyncServerConnection { get; }
        public string[] DllsToScan { get; }

        public bool ForceRescan { get; set; }
        string CurrentDbName { get; set; }
    }
}

using Hangfire;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts
{
    public interface IResumableFunctionsSettings
    {
        public IGlobalConfiguration HangfireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
        public string CurrentServiceUrl { get; }
        public int CurrentServiceId { get; internal set; }

        public IDistributedLockProvider DistributedLockProvider { get; }
        public string[] DllsToScan { get; }

        public bool ForceRescan { get; set; }
        string CurrentWaitsDbName { get; }
        string CurrentServiceName => Assembly.GetEntryAssembly().GetName().Name;

        CleanDatabaseSettings CleanDbSettings { get; }
    }
}

using Hangfire;
using Medallion.Threading;
using Medallion.Threading.WaitHandles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.TestShell
{
    internal class InMemorySettings : IResumableFunctionsSettings
    {
        public IGlobalConfiguration HangfireConfig => 
            GlobalConfiguration.Configuration.UseInMemoryStorage();

        SqliteConnection keepAliveConnection = new SqliteConnection("DataSource=:memory:");

        public DbContextOptionsBuilder WaitsDbConfig
        {
            get
            {
                keepAliveConnection.Open();
                return new DbContextOptionsBuilder().UseSqlite(keepAliveConnection);
            }
        }

        public string CurrentServiceUrl => null;

        public string[] DllsToScan => null;

        public bool ForceRescan { get; set; } = true;
        public string CurrentDbName { get; set; }
        public int CurrentServiceId { get; set; }

        public IDistributedLockProvider DistributedLockProvider => 
            new WaitHandleDistributedSynchronizationProvider();
    }
}
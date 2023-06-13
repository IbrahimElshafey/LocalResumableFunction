using Hangfire;
using Hangfire.Storage.SQLite;
using Medallion.Threading;
using Medallion.Threading.WaitHandles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.TestShell
{
    internal class InMemorySettings : IResumableFunctionsSettings
    {
        
        private readonly string _testName;
        public InMemorySettings(string testName)
        {
            _testName = testName;
        }
        public IGlobalConfiguration HangfireConfig
        {
            get
            {
                return GlobalConfiguration.Configuration.UseSQLiteStorage($"{_testName}_Hangfire.db");
            }
        }

        public DbContextOptionsBuilder WaitsDbConfig
        {
            get
            {
                return new DbContextOptionsBuilder().UseSqlite($"DataSource={_testName}_Waits.db");
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
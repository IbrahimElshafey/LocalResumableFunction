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
        private SqliteConnection _waitsConnection = new("DataSource=:memory:");
        private readonly string _testName;
        public InMemorySettings(string testName)
        {
            _testName = testName;
        }
        public IGlobalConfiguration HangfireConfig
        {
            get
            {
                return GlobalConfiguration.Configuration.UseInMemoryStorage();
                return GlobalConfiguration.Configuration.UseSQLiteStorage($"DataSource={_testName}.db");
            }
        }


       

        public DbContextOptionsBuilder WaitsDbConfig
        {
            get
            {
                _waitsConnection.Open();
                return new DbContextOptionsBuilder().UseSqlite(_waitsConnection);
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
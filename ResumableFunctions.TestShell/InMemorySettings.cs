using Hangfire;
using Medallion.Threading;
using Medallion.Threading.WaitHandles;
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
        public IGlobalConfiguration HangfireConfig => null;

        public DbContextOptionsBuilder WaitsDbConfig => new DbContextOptionsBuilder().UseSqlite($"DataSource={_testName}_Waits.db");

        public string CurrentServiceUrl => null;

        public string[] DllsToScan => null;

        public bool ForceRescan { get; set; } = true;
        public string CurrentDbName { get; set; }
        public int CurrentServiceId { get; set; }

        public IDistributedLockProvider DistributedLockProvider =>
            new WaitHandleDistributedSynchronizationProvider();
    }
}
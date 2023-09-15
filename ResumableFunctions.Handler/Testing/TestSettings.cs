using Hangfire;
using Medallion.Threading;
using Medallion.Threading.WaitHandles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.InOuts;
using System.Data.Common;
using System.Runtime;
//using System.Data.SQLite;
namespace ResumableFunctions.Handler.Testing
{
    internal class TestSettings : IResumableFunctionsSettings
    {

        private readonly string _testName;
        //public string Server = "(localdb)\\MSSQLLocalDB";
        public SqliteConnection Connection => 
            //new SqliteConnection($"DataSource=file:{_testName}?mode=memory&cache=shared");
            new SqliteConnection($"DataSource=file::memory:?cache=shared");



        public TestSettings(string testName)
        {
            _testName = testName;
            CurrentWaitsDbName = _testName;
        }
        public IGlobalConfiguration HangfireConfig => null;

        //public DbContextOptionsBuilder WaitsDbConfig =>
        //    new DbContextOptionsBuilder()
        //    .UseSqlServer($"Server={Server};Database={_testName};Trusted_Connection=True;TrustServerCertificate=True;");

        public DbContextOptionsBuilder WaitsDbConfig
        {
            get
            {
                return new DbContextOptionsBuilder().UseSqlite(Connection);
                //.UseSqlite($"file:{_testName}?mode=memory&cache=shared");
                //.UseSqlite($"DataSource=file:{_testName}?mode=memory&cache=shared");
            }
        }


        public string CurrentServiceUrl => null;

        public string[] DllsToScan => null;

        public bool ForceRescan { get; set; } = true;
        public string CurrentWaitsDbName { get; set; }
        public int CurrentServiceId { get; set; } = -1;

        //public IDistributedLockProvider DistributedLockProvider => new WaitHandleDistributedSynchronizationProvider();
        public IDistributedLockProvider DistributedLockProvider => new NoLockProvider();
        public CleanDatabaseSettings CleanDbSettings => new CleanDatabaseSettings();
        public WaitStatus WaitStatusIfProcessingError { get; set; } = WaitStatus.InError;

        private class NoLockProvider : IDistributedLockProvider
        {
            public IDistributedLock CreateLock(string name) => new NoLock();
            private class NoLock : IDistributedLock
            {
                public string Name => throw new NotImplementedException();

                public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
                {
                    return new DistributedSynchronizationHandle();
                }

                public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
                {
                    return new DistributedSynchronizationHandle();
                }

                public IDistributedSynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
                {
                    return new DistributedSynchronizationHandle();
                }

                public async ValueTask<IDistributedSynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
                {
                    return new DistributedSynchronizationHandle();
                }

                private class DistributedSynchronizationHandle : IDistributedSynchronizationHandle
                {
                    public CancellationToken HandleLostToken => CancellationToken.None;

                    public void Dispose()
                    {

                    }

                    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
                }
            }
        }
    }


}
using System.Reflection;
using Hangfire;
using Hangfire.SqlServer;
using Medallion.Threading;
using Medallion.Threading.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Handler.InOuts
{
    public class SqlServerResumableFunctionsSettings : IResumableFunctionsSettings
    {
        public string ServerName { get; } = "(localdb)\\MSSQLLocalDB;";
        public IGlobalConfiguration HangfireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }

        public string CurrentServiceUrl { get; private set; }
        public string[] DllsToScan { get; private set; }
        public bool ForceRescan { get; set; }

        //;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False
        public SqlServerResumableFunctionsSettings(string server = null, string waitsDbName = null)
        {
#if DEBUG
            ForceRescan = true;
#endif
            if (server != null)
                ServerName = server;
            waitsDbName ??= "ResumableFunctionsData";
            CreateHangfireDb();
            HangfireConfig = GlobalConfiguration
                .Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    $"Server={ServerName}" +
                    $"Database={HangfireDbName};",
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(10),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = false
                    });
            WaitsDbConfig = new DbContextOptionsBuilder()
              .UseSqlServer($"Server={ServerName};Database={waitsDbName};");
        }

        public SqlServerResumableFunctionsSettings SetDllsToScan(params string[] dlls)
        {
            DllsToScan = dlls;
            return this;
        }

        public SqlServerResumableFunctionsSettings SetCurrentServiceUrl(string serviceUrl)
        {
            CurrentServiceUrl = serviceUrl;
            return this;
        }

        private string HangfireDbName => $"{Assembly.GetEntryAssembly().GetName().Name}_HangfireDb".Replace(".", "_");

        public long CurrentServiceId { get; set; } = -1;
        public string CurrentDbName { get; set; }

        public IDistributedLockProvider DistributedLockProvider =>
            new SqlDistributedSynchronizationProvider($"Data Source={ServerName};Initial Catalog=master;Integrated Security=True");

        private void CreateHangfireDb()
        {
            var dbConfig = new DbContextOptionsBuilder()
              .UseSqlServer($"Server={ServerName};Database={HangfireDbName};");
            var context = new DbContext(dbConfig.Options);
            try
            {
                using var loc = DistributedLockProvider.AcquireLock(HangfireDbName);
                context.Database.EnsureCreated();
            }
            catch (Exception)
            {
            }
        }

    }
}

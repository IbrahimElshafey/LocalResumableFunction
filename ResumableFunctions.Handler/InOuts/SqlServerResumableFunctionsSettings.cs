using System.Reflection;
using Hangfire;
using Hangfire.SqlServer;
using Medallion.Threading;
using Medallion.Threading.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Handler.InOuts
{
    public class SqlServerResumableFunctionsSettings : IResumableFunctionsSettings
    {
        private const string LocalDbServer = "(localdb)\\MSSQLLocalDB;";
        public IGlobalConfiguration HangfireConfig { get; private set; }
        public DbContextOptionsBuilder WaitsDbConfig { get; private set; }
        public SqlConnectionStringBuilder ConnectionBuilder { get; }

        public string CurrentServiceUrl { get; private set; }
        public string[] DllsToScan { get; private set; }
        public bool ForceRescan { get; set; }


        //;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False
        public SqlServerResumableFunctionsSettings(SqlConnectionStringBuilder connectionBuilder = null, string waitsDbName = null, string hangfireDbName = null)
        {
#if DEBUG
            ForceRescan = true;
#endif
            if (connectionBuilder != null)
                ConnectionBuilder = connectionBuilder;
            else
            {
                ConnectionBuilder = new SqlConnectionStringBuilder("Server=(localdb)\\MSSQLLocalDB");
                ConnectionBuilder["Trusted_Connection"] = "yes";
            }

            SetWaitsDbConfig(waitsDbName);
            SetHangfireConfig(hangfireDbName);
        }

        private void SetWaitsDbConfig(string waitsDbName)
        {
            waitsDbName ??= "ResumableFunctionsData";
            ConnectionBuilder["Database"] = waitsDbName;
            WaitsDbConfig = new DbContextOptionsBuilder().UseSqlServer(ConnectionBuilder.ConnectionString);
        }

        private void SetHangfireConfig(string dbName)
        {
            var hangfireDbName = dbName ?? $"{Assembly.GetEntryAssembly().GetName().Name}_HangfireDb".Replace(".", "_");
            
            CreateEmptyHangfireDb(hangfireDbName);
            
            ConnectionBuilder["Database"] = hangfireDbName;
           
            HangfireConfig = GlobalConfiguration
                .Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    ConnectionBuilder.ConnectionString,
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(10),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = false
                    });
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


        public long CurrentServiceId { get; set; } = -1;
        public string CurrentDbName { get; set; }

        public IDistributedLockProvider DistributedLockProvider
        {
            get
            {
                ConnectionBuilder.InitialCatalog = "master";
                return new SqlDistributedSynchronizationProvider(ConnectionBuilder.ConnectionString);
            }
        }

        private void CreateEmptyHangfireDb(string hangfireDbName)
        {
            ConnectionBuilder["Database"] = hangfireDbName;
            var dbConfig = new DbContextOptionsBuilder().UseSqlServer(ConnectionBuilder.ConnectionString);
            var context = new DbContext(dbConfig.Options);
            try
            {
                using var loc = DistributedLockProvider.AcquireLock(hangfireDbName);
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                //todo:log error
            }
        }

    }
}

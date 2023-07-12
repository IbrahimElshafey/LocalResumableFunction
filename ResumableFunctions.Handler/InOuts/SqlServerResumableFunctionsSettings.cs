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
        public IGlobalConfiguration HangfireConfig { get; private set; }
        public DbContextOptionsBuilder WaitsDbConfig { get; private set; }
        private readonly SqlConnectionStringBuilder _connectionBuilder;

        public string CurrentServiceUrl { get; private set; }
        public string[] DllsToScan { get; private set; }
        public bool ForceRescan { get; set; }


        //;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False
        public SqlServerResumableFunctionsSettings(
            SqlConnectionStringBuilder connectionBuilder = null,
            string waitsDbName = null,
            string hangfireDbName = null)
        {
#if DEBUG
            ForceRescan = true;
#endif
            if (connectionBuilder != null)
                _connectionBuilder = connectionBuilder;
            else
            {
                _connectionBuilder = new SqlConnectionStringBuilder("Server=(localdb)\\MSSQLLocalDB");
                _connectionBuilder["Trusted_Connection"] = "yes";
            }

            SetWaitsDbConfig(waitsDbName);
            SetHangfireConfig(hangfireDbName);
        }

        private void SetWaitsDbConfig(string waitsDbName)
        {
            waitsDbName ??= "ResumableFunctionsData";
            _connectionBuilder["Database"] = waitsDbName;
            WaitsDbConfig = new DbContextOptionsBuilder().UseSqlServer(_connectionBuilder.ConnectionString);
        }

        private void SetHangfireConfig(string dbName)
        {
            var hangfireDbName = dbName ?? $"{Assembly.GetEntryAssembly().GetName().Name}_HangfireDb".Replace(".", "_");
            
            CreateEmptyHangfireDb(hangfireDbName);
            
            _connectionBuilder["Database"] = hangfireDbName;
           
            HangfireConfig = GlobalConfiguration
                .Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    _connectionBuilder.ConnectionString,
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


        public int CurrentServiceId { get; set; } = -1;
        public string CurrentDbName { get; set; }

        public IDistributedLockProvider DistributedLockProvider
        {
            get
            {
                _connectionBuilder.InitialCatalog = "master";
                return new SqlDistributedSynchronizationProvider(_connectionBuilder.ConnectionString);
            }
        }

        public CleanDatabaseSettings CleanDbSettings => new CleanDatabaseSettings();

        private void CreateEmptyHangfireDb(string hangfireDbName)
        {
            _connectionBuilder["Database"] = hangfireDbName;
            var dbConfig = new DbContextOptionsBuilder().UseSqlServer(_connectionBuilder.ConnectionString);
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

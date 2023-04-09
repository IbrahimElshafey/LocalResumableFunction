using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts
{
    public class ResumableFunctionsSettings : IResumableFunctionsSettings
    {
        private const string localDbServer = "(localdb)\\MSSQLLocalDB;";

        public ResumableFunctionsSettings SetDllsToScan(params string[] dlls)
        {
            DllsToScan = dlls;
            return this;
        }

        public ResumableFunctionsSettings SetCurrentServiceUrl(string serviceUrl)
        {
            CurrentServiceUrl = serviceUrl;
            return this;
        }

        public ResumableFunctionsSettings UseSqlServer(string server = null)
        {
            if (server == null)
                server = localDbServer;
            HangFireConfig = GlobalConfiguration
                .Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    $"Server={server}" +
                    $"Database={Assembly.GetEntryAssembly().GetName().Name}_HangfireDb;",
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(10),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = false
                    });
            WaitsDbConfig = new DbContextOptionsBuilder()
              .UseSqlServer($"Server={server};Database=ResumableFunctionsData;");
            return this;
        }

        public IGlobalConfiguration HangFireConfig { get; private set; }
        public DbContextOptionsBuilder WaitsDbConfig { get; private set; }

        public string CurrentServiceUrl { get; private set; }
        public string[] DllsToScan { get; private set; }
        public bool ForceRescan { get; set; }
    }
}

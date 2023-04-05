using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ResumableFunctions.Core.InOuts
{
    public class ResumableFunctionSettings : IResumableFunctionSettings
    {
        private IGlobalConfiguration hangFireConfig = GlobalConfiguration
            .Configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage($"Server=(localdb)\\MSSQLLocalDB;Database={Assembly.GetEntryAssembly().GetName().Name}_HangfireDb;", new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(10),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = false
            });
        private DbContextOptionsBuilder waitsDbConfig = new DbContextOptionsBuilder()
              .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");

        public IGlobalConfiguration HangFireConfig
        {
            get => hangFireConfig;
            set => hangFireConfig = value;
        }
        public DbContextOptionsBuilder WaitsDbConfig
        {
            get => waitsDbConfig;
            set => waitsDbConfig = value;
        }

        public string CurrentServiceUrl { get; set; }
        public string[] DllsToScan { get; set; }
        public bool ForceRescan { get; set; }
    }
}

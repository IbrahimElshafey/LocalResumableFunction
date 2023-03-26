using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ResumableFunctions.Core
{
    public class DefaultResumableFunctionSettings : IResumableFunctionSettings
    {
        public IGlobalConfiguration HangFireConfig => GlobalConfiguration
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

        public DbContextOptionsBuilder WaitsDbConfig => new DbContextOptionsBuilder()
              .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");
    }
}

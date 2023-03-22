using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Core.Abstraction;

public class Test1RfSettings : IResumableFunctionSettings
{
    public IGlobalConfiguration HangFireConfig => GlobalConfiguration
        .Configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage($"Server=(localdb)\\MSSQLLocalDB;Database=HangfireDb;", new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        });

    public DbContextOptionsBuilder WaitsDbConfig => new DbContextOptionsBuilder()
          .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");
}
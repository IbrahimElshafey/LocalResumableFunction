using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts
{
    public class SqlServerResumableFunctionsSettings : IResumableFunctionsSettings
    {
        public string ServerName { get; private set; } = "(localdb)\\MSSQLLocalDB;";
        public IGlobalConfiguration HangFireConfig { get; private set; }
        public DbContextOptionsBuilder WaitsDbConfig { get; private set; }

        public string CurrentServiceUrl { get; private set; }
        public string[] DllsToScan { get; private set; }
        public bool ForceRescan { get; set; }

        //;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False
        public string SyncServerConnection => $"Data Source={ServerName};Initial Catalog=master;Integrated Security=True";
        public SqlServerResumableFunctionsSettings(string server = null, string waitsDbName = null)
        {
            if (server != null)
                ServerName = server;
            if (waitsDbName == null)
                waitsDbName = "ResumableFunctionsData";
            CreateHangfireDb();
            HangFireConfig = GlobalConfiguration
                .Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    $"Server={ServerName}" +
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


        private void CreateHangfireDb()
        {
            var hangFireDbName = $"{Assembly.GetEntryAssembly().GetName().Name}_HangfireDb";
            var dbConfig = new DbContextOptionsBuilder()
              .UseSqlServer($"Server={ServerName};Database={hangFireDbName};");
            var context = new DbContext(dbConfig.Options);
            try
            {
                context.Database.EnsureCreated();//todo:how to pass IDistributedLockProvider and lock
            }
            catch (Exception)
            {
            }
        }

    }
}

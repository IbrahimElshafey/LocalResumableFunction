// See https://aka.ms/new-console-template for more information

using Hangfire;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Core.Abstraction;

namespace Test;

public class ResumableFunctionSettings : IResumableFunctionSettings
{
    public IGlobalConfiguration HangFireConfig => GlobalConfiguration
          .Configuration
          .UseSqlServerStorage("Server=(localdb)\\MSSQLLocalDB;Database=HangfireDb;");

    public DbContextOptionsBuilder WaitsDbConfig => new DbContextOptionsBuilder()
          .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");
}
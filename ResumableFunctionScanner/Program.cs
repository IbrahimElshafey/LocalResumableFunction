using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;
using System.Diagnostics;

namespace ResumableFunctionScanner;

internal class Program
{
    public static async Task Main(string[] args)
    {

        using IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services => services.AddResumableFunctionsCore(new ResumableFunctionSettings()))
        .Build();
        //todo:Scanner should create file for [ExternalWaitMethod] matched to faclitate using 
        await StartScan(host.Services);
        await host.RunAsync();
    }

    private static async Task StartScan(IServiceProvider services)
    {
        Extensions.SetServiceProvider(services);
        await services.GetService<Scanner>().Start();
    }
}
public class ResumableFunctionSettings : IResumableFunctionSettings
{
    public IGlobalConfiguration HangFireConfig => null;

    public DbContextOptionsBuilder WaitsDbConfig => new DbContextOptionsBuilder()
          .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");
}
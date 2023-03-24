using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;
using System;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void ScanCurrentService(this WebApplication app,string serviceUrl)
        {
            CoreExtensions.SetServiceProvider(app.Services);

            GlobalConfiguration.Configuration
              .UseActivator(new HangfireActivator());

            var scannerScope = app.Services.CreateScope();
            var backgroundJobClient = scannerScope.ServiceProvider.GetService<IBackgroundJobClient>();
            var scanner = scannerScope.ServiceProvider.GetService<Scanner>();
            backgroundJobClient.Enqueue(() => scanner.Start(serviceUrl));
        }

        public static void AddResumableFunctions(this IMvcBuilder mvcBuilder, IResumableFunctionSettings settings)
        {
            mvcBuilder
                .AddApplicationPart(typeof(ResumableFunctionsReceiverController).Assembly)
                .AddControllersAsServices();
            mvcBuilder.Services.AddResumableFunctionsCore(settings);
        }
    }
}
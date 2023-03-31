using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using System;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void ScanCurrentService(this WebApplication app)
        {
            CoreExtensions.SetServiceProvider(app.Services);

            GlobalConfiguration.Configuration
              .UseActivator(new HangfireActivator());

            var backgroundJobClient = app.Services.GetService<IBackgroundJobClient>();
            var scanner = app.Services.GetService<Scanner>();
            backgroundJobClient.Enqueue(() => scanner.Start());
            app.UseHangfireDashboard();
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
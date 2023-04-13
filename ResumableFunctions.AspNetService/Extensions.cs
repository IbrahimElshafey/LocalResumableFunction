using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void AddResumableFunctions(this IMvcBuilder mvcBuilder, IResumableFunctionsSettings settings)
        {
            mvcBuilder
                .AddApplicationPart(typeof(ResumableFunctionsController).Assembly)
                .AddControllersAsServices();
            mvcBuilder.Services.AddRazorPages();
            mvcBuilder.Services.AddResumableFunctionsCore(settings);
        }

        public static void ScanCurrentService(this WebApplication app)
        {
            CoreExtensions.SetServiceProvider(app.Services);

            GlobalConfiguration.Configuration
              .UseActivator(new HangfireActivator());

            var backgroundJobClient = app.Services.GetService<IBackgroundJobClient>();
            var scanner = app.Services.GetService<Scanner>();
            backgroundJobClient.Enqueue(() => scanner.Start());
            app.UseHangfireDashboard();
            app.MapRazorPages();
        }   
    }
}
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.Helpers;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void ScanCurrentService(this WebApplication app)
        {
            var backgroundJobClient = app.Services.GetService<IBackgroundJobClient>();
            var scanner = app.Services.GetService<Scanner>();
            backgroundJobClient.Enqueue(() => scanner.Start());
        }

        public static void AddResumableFunctions(this IMvcBuilder mvcBuilder, IResumableFunctionSettings settings)
        {
            mvcBuilder
                .AddApplicationPart(typeof(ResumableFunctionsReceiverController).Assembly)
                .AddControllersAsServices();
            mvcBuilder.Services.AddResumableFunctionsCore(settings);
        }
    }

    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type) => _serviceProvider.GetService(type);
    }
}
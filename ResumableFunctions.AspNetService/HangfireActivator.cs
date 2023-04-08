using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.AspNetService
{
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator()
        {
            _serviceProvider = CoreExtensions.GetServiceProvider().CreateScope().ServiceProvider;
        }

        public override object ActivateJob(Type type)
        {
            return _serviceProvider.GetService(type) ?? ActivatorUtilities.CreateInstance(_serviceProvider, type);
        }
    }
}
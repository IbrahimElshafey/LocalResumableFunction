using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.Implementation;

namespace ResumableFunctions.AspNetService
{
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator()
        {
            _serviceProvider = CoreExtensions.GetServiceProvider();
        }

        public override object ActivateJob(Type type)
        {
            return _serviceProvider.GetService(type);
        }
    }
}
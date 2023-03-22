using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.AspNetService
{
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator(WebApplication webApplication)
        {
            _serviceProvider = webApplication.Services.CreateScope().ServiceProvider;
        }

        public override object ActivateJob(Type type) => _serviceProvider.GetService(type);
    }
}
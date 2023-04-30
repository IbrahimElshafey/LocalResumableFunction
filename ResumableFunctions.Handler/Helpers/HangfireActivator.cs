using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Handler.Helpers;

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
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Handler.Helpers;

public class HangfireActivator : JobActivator
{
    internal static IServiceProvider ServiceProvider;

    public override object ActivateJob(Type type)
    {
        return ServiceProvider.CreateScope().ServiceProvider.GetService(type) ?? 
            ActivatorUtilities.CreateInstance(ServiceProvider, type);
    }
}
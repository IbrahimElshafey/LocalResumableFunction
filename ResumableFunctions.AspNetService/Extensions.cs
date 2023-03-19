using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void AddResumableFunctions(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.AddApplicationPart(typeof(ResumableFunctionReceiverController).Assembly).AddControllersAsServices();
            mvcBuilder.Services.AddHostedService<QueuedHostedService>();
            mvcBuilder.Services.AddSingleton<IBackgroundTaskQueue>( _ =>
            {
                //if (!int.TryParse(hostContext.Configuration["QueueCapacity"], out var queueCapacity))
                //    queueCapacity = 100;
                return new BackgroundTaskQueue(100);
            });
        }
    }
}
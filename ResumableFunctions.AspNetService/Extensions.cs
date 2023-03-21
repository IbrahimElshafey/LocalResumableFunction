using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.Data;

namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void AddResumableFunctions(this IMvcBuilder mvcBuilder, ResumableFunctionSettings settings)
        {
            var services = mvcBuilder.Services;
            mvcBuilder.AddApplicationPart(typeof(ResumableFunctionReceiverController).Assembly).AddControllersAsServices();
            services.AddScoped<IPushMethodCall, ResumableFunctionHandler>();
            services.AddDbContext<FunctionDataContext>(settings.WaitsDbConfig);
            services.AddHangfire(settings.HangFireConfig);
            services.AddHangfireServer();
            //app.UseHangfireDashboard();
        }
    }
}
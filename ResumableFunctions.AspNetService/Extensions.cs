using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
namespace ResumableFunctions.AspNetService
{
    public static class Extensions
    {
        public static void AddResumableFunctionsUi(this IMvcBuilder mvcBuilder, IResumableFunctionsSettings settings)
        {
            mvcBuilder
                .AddApplicationPart(typeof(ResumableFunctionsController).Assembly)
                .AddControllersAsServices();
            mvcBuilder.Services.AddRazorPages();
            mvcBuilder.Services.AddResumableFunctionsCore(settings);
        }


        public static void UseResumableFunctionsUi(this WebApplication app)
        {

            CoreExtensions.UseResumableFunctions(app);
            app.UseHangfireDashboard();
            app.MapRazorPages();
            app.UseStaticFiles();


            app.UseRouting();

            app.MapControllerRoute(
                name: "MyArea",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

        }
    }
}
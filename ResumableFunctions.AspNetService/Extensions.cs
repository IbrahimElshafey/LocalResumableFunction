using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
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
            //CreateHangfireDb(mvcBuilder, settings);
        }

        
        public static void UseResumableFunctions(this WebApplication app)
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

        public static T ToObject<T>(this Stream stream)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }
    }
}
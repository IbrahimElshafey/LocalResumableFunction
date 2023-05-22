using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Publisher
{
    public static class Extensions
    {
        //private static IServiceProvider _ServiceProvider;
        //public static IServiceProvider GetServiceProvider() => _ServiceProvider;
        //public static void SetServiceProvider(IServiceProvider provider) => _ServiceProvider = provider;


        public static void AddResumableFunctionsPublisher(this IServiceCollection services, IPublisherSettings settings)
        {
            services.AddSingleton(typeof(IPublisherSettings), settings);
            services.AddSingleton(typeof(IPublishCall), settings.PublishCallImplementation);
            services.AddSingleton<HttpClient>();
        }

        public static void UseResumableFunctionsPublisher(this IHost app)
        {
            PublishMethodAspect.ServiceProvider = app.Services;
        }

        public static bool IsAsyncMethod(this MethodBase method)
        {
            var attType = typeof(AsyncStateMachineAttribute);

            // Obtain the custom attribute for the method. 
            // The value returned contains the StateMachineType property. 
            // Null is returned if the attribute isn't present for the method. 
            var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);


            if (attrib == null)
            {
                return
                  attrib == null &&
                  method is MethodInfo mi &&
                  mi != null &&
                  mi.ReturnType.IsGenericType &&
                  mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            }
            return true;
        }


        public static string GetFullName(this MethodBase method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
    }
}
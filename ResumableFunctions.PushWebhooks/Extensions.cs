using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.PublishWebhooks
{
    public static class Extensions
    {
        //IHost, IApplicationBuilder,
        public static void ScanCurrentService(this IHost app)
        {
            SetServiceProvider(app.Services);
        }

        private static IServiceProvider _serviceProvider;
        internal static IServiceProvider GetServiceProvider() => _serviceProvider;
        internal static void SetServiceProvider(IServiceProvider provider) => _serviceProvider = provider;
        
        public static bool IsAsyncMethod(this MethodBase method)
        {
            var attType = typeof(AsyncStateMachineAttribute);

            // Obtain the custom attribute for the method. 
            // The value returned contains the StateMachineType property. 
            // Null is returned if the attribute isn't present for the method. 
            var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);


            if (attrib == null)
            {
                bool returnTypeIsTask =
                  attrib == null &&
                  method is MethodInfo mi &&
                  mi != null &&
                  mi.ReturnType.IsGenericType &&
                  mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
                return returnTypeIsTask;
            }
            return true;
        }

        public static string GetFullName(this MethodBase method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
    }
}

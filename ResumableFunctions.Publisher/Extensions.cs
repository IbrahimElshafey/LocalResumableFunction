﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Http;
namespace ResumableFunctions.Publisher
{
    public static class Extensions
    {
        public static void AddResumableFunctionsPublisher(this IServiceCollection services, IPublisherSettings settings)
        {
            services.AddSingleton(typeof(IPublisherSettings), settings);
            services.AddHttpClient();
            services.AddSingleton(typeof(ICallPublisher), settings.CallPublisherType);
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
            var asyncAttr = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);


            if (asyncAttr == null)
            {
                return
                  asyncAttr == null &&
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
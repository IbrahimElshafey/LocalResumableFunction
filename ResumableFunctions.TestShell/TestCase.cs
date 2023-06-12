using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.TestShell
{
    public class TestCase
    {
        private IHost _app;
        private readonly HostApplicationBuilder _builder;
        private readonly Type[] _types;
        private readonly string _testName;

        public TestCase(string testName, params Type[] types)
        {
            _builder = Host.CreateApplicationBuilder();
            _builder.Services.AddResumableFunctionsCore(new InMemorySettings());
            _types = types;
            _testName = testName;
        }

        public async Task Initialize()
        {
            _app = _builder.Build();
            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(_app.Services));
            await ScanTypes();
            //_app.Run();
        }

        private async Task ScanTypes()
        {
            using var scope = _app.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<FunctionDataContext>();
            var serviceData = new ServiceData
            {
                AssemblyName = _types[0].Assembly.GetName().Name,
                ParentId = -1,
            };
            context.ServicesData.Add(serviceData);
            context.SaveChanges();
            var scanner = scope.ServiceProvider.GetService<Scanner>();
            foreach (var type in _types)
                await scanner.RegisterMethods(type, serviceData);
            foreach (var type in _types)
                if (type.IsSubclassOf(typeof(ResumableFunction)))
                    await scanner.RegisterResumableFunctionsInClass(type);
        }

        public async Task SimulateMethodCall<ClassType, Input, Output>(
            Expression<Func<ClassType, object>> methodSelector,
            Input input,
            Output outPut)
        {
            var methodInfo = CoreExtensions.GetMethodInfo(methodSelector).MethodInfo;
            var pusher = _app.Services.GetService<ICallPusher>();
            var pushResultAttribute = methodInfo.GetCustomAttribute<PushCallAttribute>();
            await pusher.PushCall(
                new PushedCall
                {
                    Data =
                    {
                        Input= input,
                        Output= outPut//may be async task
                    },
                    MethodData = new MethodData(methodInfo)
                    {
                        MethodUrn = pushResultAttribute.MethodUrn,
                        CanPublishFromExternal = pushResultAttribute.CanPublishFromExternal,
                    },
                });
        }
    }


}

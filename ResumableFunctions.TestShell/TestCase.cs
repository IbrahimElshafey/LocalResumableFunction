using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
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
        private IHost app;
        private readonly HostApplicationBuilder _builder;
        private readonly Type[] _types;

        public TestCase(params Type[] types)
        {
            _builder = Host.CreateApplicationBuilder();
            _builder.Services.AddResumableFunctionsCore(new InMemorySettings());
            _types = types;
        }

        public async Task Start()
        {
            app = _builder.Build();
            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(app.Services));
            await ScanTypes();
            app.Run();
        }

        private async Task ScanTypes()
        {
            var backgroundJobClient = app.Services.GetService<IBackgroundJobClient>();
            var scanner = app.Services.GetService<Scanner>();
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
            var methodInfo = CoreExtensions.GetMethodInfo(methodSelector);
            var pusher = app.Services.GetService<ICallPusher>();
            await pusher.PushCall(new PushedCall { });
        }
    }


    internal class InMemorySettings : IResumableFunctionsSettings
    {
    }
}

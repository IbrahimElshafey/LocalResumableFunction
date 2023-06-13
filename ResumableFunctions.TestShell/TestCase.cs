using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.CompilerServices.TypeSystem;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ResumableFunctions.TestShell
{
    public class TestCase
    {
        public IHost CurrentApp { get; private set; }

        private HostApplicationBuilder _builder;
        private readonly Type[] _types;
        private readonly string _testName;
        private readonly InMemorySettings _settings;

        public TestCase(string testName, params Type[] types)
        {
            _testName = testName;
            _settings = new InMemorySettings(testName);
            SetBuilder();
            CancelSqliteEditor();
            DeleteDbs();
            _types = types;
        }

        private void CancelSqliteEditor()
        {
            foreach (var process in Process.GetProcessesByName("DB Browser for SQLite"))
            {
                 process.Kill(true);
            }
        }

        private void SetBuilder()
        {
            _builder = Host.CreateApplicationBuilder();
            _builder.Services.AddResumableFunctionsCore(_settings);
        }

        private void DeleteDbs()
        {
            if (File.Exists($"{_testName}_Hangfire.db"))
                File.Delete($"{_testName}_Hangfire.db");
            if (File.Exists($"{_testName}_Waits.db"))
                File.Delete($"{_testName}_Waits.db");
        }

        public IServiceCollection RegisteredServices => _builder.Services;

        public async Task ScanTypes()
        {
            CurrentApp = _builder.Build();
            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(CurrentApp.Services));

            using var scope = CurrentApp.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<FunctionDataContext>();
            var serviceData = new ServiceData
            {
                AssemblyName = _types[0].Assembly.GetName().Name,
                ParentId = -1,
            };
            context.ServicesData.Add(serviceData);
            context.SaveChanges();
            _settings.CurrentServiceId = serviceData.Id;
            var scanner = scope.ServiceProvider.GetService<Scanner>();
            foreach (var type in _types)
                await scanner.RegisterMethods(type, serviceData);
            foreach (var type in _types)
                if (type.IsSubclassOf(typeof(ResumableFunction)))
                    await scanner.RegisterResumableFunctionsInClass(type);
        }



        public async Task<int> SimulateMethodCall<ClassType, Input, Output>(
            Expression<Func<ClassType, object>> methodSelector,
            Input input,
            Output outPut)
        {
            var methodInfo = CoreExtensions.GetMethodInfo(methodSelector).MethodInfo;
            //todo:check input and output match method signature
            var pusher = CurrentApp.Services.GetService<ICallPusher>();
            var pushResultAttribute = methodInfo.GetCustomAttribute<PushCallAttribute>();
            var pushedCallId = await pusher.PushCall(
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
            return pushedCallId;
        }

        private FunctionDataContext _context => CurrentApp.Services.GetService<FunctionDataContext>();
        public async Task<List<ResumableFunctionState>> GetInstances<T>()
        {
            var instances = await _context.FunctionStates.Where(x => x.Status != FunctionStatus.New).ToListAsync();
            foreach (var instnace in instances)
            {
                instnace.LoadUnmappedProps(typeof(T));
            }
            return instances;
        }

        public async Task<List<PushedCall>> GetPushedCalls()
        {
            var calls = await _context.PushedCalls.ToListAsync();
            foreach (var call in calls)
            {
                call.LoadUnmappedProps();
            }
            return calls;
        }

        public async Task UpdateData(params object[] objects)
        {
            foreach (var item in objects)
            {
                _context.Entry(item).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Wait>> GetWaits(int? instanceId = null)
        {
            var query = _context.Waits.AsQueryable();
            if (instanceId != null)
                query = query.Where(x => x.FunctionStateId == instanceId);
            return await query.ToListAsync();
        }
    }
}

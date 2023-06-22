using FastExpressionCompiler;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.Helpers.Expressions;
using ResumableFunctions.Handler.InOuts;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

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
            await scanner.RegisterMethods(typeof(LocalRegisteredMethods), serviceData);
            foreach (var type in _types)
                if (type.IsSubclassOf(typeof(ResumableFunction)))
                    await scanner.RegisterResumableFunctionsInClass(type);
        }


        //todo:enhance this to take paramters in method call
        public async Task<int> SimulateMethodCall<ClassType>(
           Expression<Func<ClassType, object>> methodSelector,
           object output)
        {
            object input = null;
            var inputVisitor = new GenericVisitor();
            inputVisitor.OnVisitCall(call =>
            {
                input = Expression.Lambda(call.Arguments[0]).CompileFast().DynamicInvoke();
                return call;
            });
            inputVisitor.Visit(methodSelector);
            if (input != null)
                return await SimulateMethodCall(methodSelector, input, output);
            else
                throw new Exception("Can't get input");
        }

        public async Task<int> SimulateMethodCall<ClassType>(
            Expression<Func<ClassType, object>> methodSelector,
            object input,
            object output)
        {
            var methodInfo = CoreExtensions.GetMethodInfo(methodSelector);
            var pusher = CurrentApp.Services.GetService<ICallPusher>();
            var pushResultAttribute = methodInfo.GetCustomAttribute<PushCallAttribute>();
            var pushedCallId = await pusher.PushCall(
                new PushedCall
                {
                    Data =
                    {
                        Input= input,
                        Output= output//may be async task
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
        public async Task<List<ResumableFunctionState>> GetInstances<T>(bool includeNew = false)
        {
            var query = _context.FunctionStates.AsQueryable();
            if (includeNew is false)
            {
                query = query.Where(x => x.Status != FunctionStatus.New);
            }
            var instances = await query.ToListAsync();
            foreach (var instnace in instances)
            {
                await _context.Entry(instnace).ReloadAsync();
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


        public async Task<List<Wait>> GetWaits(int? instanceId = null, bool includeFirst = false)
        {
            var query = _context.Waits.AsQueryable();
            if (instanceId != null)
                query = query.Where(x => x.FunctionStateId == instanceId);
            if (includeFirst is false)
                query = query.Where(x => !x.IsFirst);
            return await query.OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<List<LogRecord>> GetErrors()
        {
            return
                await _context.Logs
                .Where(x => x.Type == LogType.Error)
                .ToListAsync();
        }


    }
}

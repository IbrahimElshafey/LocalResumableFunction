using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Testing
{
    public class TestShell:IDisposable
    {
        public IHost CurrentApp { get; private set; }
        private readonly HostApplicationBuilder _builder;
        private readonly Type[] _types;
        private readonly TestSettings _settings;
        private readonly string _testName;
        //(localdb)\\MSSQLLocalDB
        private const string Server= ".\\SQLEXPRESS";
        public TestShell(string testName, params Type[] types)
        {
            _testName = testName;
            _settings = new TestSettings(testName);
            _builder = Host.CreateApplicationBuilder();
            _types = types;
        }
        
        public static async Task DeleteDb(string dbName)
        {
            var dbConfig = new DbContextOptionsBuilder()
                .UseSqlServer(
                    $"Server={Server};Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;");
            var context = new DbContext(dbConfig.Options);
            try
            {
                await context.Database.EnsureDeletedAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public IServiceCollection RegisteredServices => _builder.Services;

        public async Task ScanTypes()
        {
            await DeleteDb(_testName);
            _builder.Services.AddResumableFunctionsCore(_settings);
            CurrentApp = _builder.Build();
            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(CurrentApp.Services));

            using var scope = CurrentApp.Services.CreateScope();
            var serviceData = new ServiceData
            {
                AssemblyName = _types[0].Assembly.GetName().Name,
                ParentId = -1,
            };
            await using var context = scope.ServiceProvider.GetService<WaitsDataContext>();
            context.ServicesData.Add(serviceData);
            await context.SaveChangesAsync();
            _settings.CurrentServiceId = serviceData.Id;
            var scanner = scope.ServiceProvider.GetService<Scanner>();
            foreach (var type in _types)
                await scanner.RegisterMethods(type, serviceData);
            await scanner.RegisterMethods(typeof(LocalRegisteredMethods), serviceData);
            await context.SaveChangesAsync();

            foreach (var type in _types)
                if (type.IsSubclassOf(typeof(ResumableFunction)))
                    await scanner.RegisterResumableFunctionsInClass(type);
            await context.SaveChangesAsync();
            await context.DisposeAsync();
        }


        public async Task<int> SimulateMethodCall<TClassType>(
           Expression<Func<TClassType, object>> methodSelector,
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

        public async Task<int> SimulateMethodCall<TClassType>(
            Expression<Func<TClassType, object>> methodSelector,
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
            await Context.SaveChangesAsync();
            return pushedCallId;
        }

        private WaitsDataContext Context => CurrentApp.Services.GetService<WaitsDataContext>();
        public async Task<List<ResumableFunctionState>> GetInstances<T>(bool includeNew = false)
        {
            var query = Context.FunctionStates.AsQueryable().AsNoTracking();
            if (includeNew is false)
            {
                query = query.Where(x => x.Status != FunctionStatus.New);
            }
            var instances = await query.ToListAsync();
            foreach (var instance in instances)
            {
                //await Context.Entry(instance).ReloadAsync();
                instance.LoadUnmappedProps(typeof(T));

            }
            return instances;
        }

        public async Task<List<PushedCall>> GetPushedCalls()
        {
            var calls = await Context.PushedCalls.AsNoTracking().ToListAsync();
            foreach (var call in calls)
            {
                call.LoadUnmappedProps();
            }
            return calls;
        }


        public async Task<List<Wait>> GetWaits(int? instanceId = null, bool includeFirst = false)
        {
            var query = Context.Waits.AsQueryable().AsNoTracking();
            if (instanceId != null)
                query = query.Where(x => x.FunctionStateId == instanceId);
            if (includeFirst is false)
                query = query.Where(x => !x.IsFirst);
            return await query.OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<List<LogRecord>> GetLogs(LogType logType = LogType.Error)
        {
            return
                await Context.Logs
                    .Where(x => x.Type == logType)
                    .AsNoTracking()
                    .ToListAsync();
        }

        public void Dispose()
        {
            Context?.Dispose();
            CurrentApp?.Dispose();
        }
    }
}

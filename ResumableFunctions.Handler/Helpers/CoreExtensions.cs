using Hangfire;
using Hangfire.Logging;
using Hangfire.SqlServer;
using Medallion.Threading;
using Medallion.Threading.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

public static class CoreExtensions
{
    //internal static IServiceProvider GetServiceProvider() => _ServiceProvider;
    public static void AddResumableFunctionsCore(this IServiceCollection services, IResumableFunctionsSettings settings)
    {


        services.AddDbContext<FunctionDataContext>(x => x = settings.WaitsDbConfig);
        services.AddScoped<IMethodIdentifierRepository, MethodIdentifierRepository>();
        services.AddScoped<IWaitsRepository, WaitsRepository>();

        services.AddScoped<IFirstWaitProcessor, FirstWaitProcessor>();
        services.AddScoped<IRecycleBinService, RecycleBinService>();
        services.AddScoped<IReplayWaitProcessor, ReplayWaitProcessor>();
        services.AddScoped<IWaitProcessor, WaitProcessor>();
        services.AddScoped<Scanner>();


        services.AddScoped<IPushedCallProcessor, PushedCallProcessor>();


        services.AddSingleton<HttpClient>();
        services.AddSingleton<HangFireHttpClient>();
        services.AddSingleton(typeof(IResumableFunctionsSettings), settings);
        services.AddSingleton<IDistributedLockProvider>(_ => new SqlDistributedSynchronizationProvider(settings.SyncServerConnection));


        services.AddScoped<IUiService, UiService.UiService>();
        if (settings.HangFireConfig != null)
        {
            services.AddHangfire(x => x = settings.HangFireConfig);
            services.AddHangfireServer();
        }
    }

    public static void UseResumableFunctions(this IHost app)
    {
        WaitMethodAspect.ServiceProvider = app.Services;
        HangfireActivator.ServiceProvider = app.Services;
        GlobalConfiguration.Configuration
          .UseActivator(new HangfireActivator());

        EnqueueScanProcess(app);
    }

   
    private static void EnqueueScanProcess(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetService<IBackgroundJobClient>();
        var scanner = scope.ServiceProvider.GetService<Scanner>();
        backgroundJobClient.Enqueue(() => scanner.Start());
    }


    public static (bool IsFunctionData, MemberExpression NewExpression) GetDataParamterAccess(
        this MemberExpression node,
        ParameterExpression functionInstanceArg)
    {
        var propAccessStack = new Stack<MemberInfo>();
        var isFunctionData = IsDataAccess(node);
        if (isFunctionData)
        {
            var newAccess = MakeMemberAccess(functionInstanceArg, propAccessStack.Pop());
            while (propAccessStack.Count > 0)
            {
                var currentProp = propAccessStack.Pop();
                newAccess = MakeMemberAccess(newAccess, currentProp);
            }

            return (true, newAccess);
        }

        return (false, null);

        bool IsDataAccess(MemberExpression currentNode)
        {
            propAccessStack.Push(currentNode.Member);
            var subNode = currentNode.Expression;
            if (subNode == null) return false;
            //is function data access 
            var isFunctionDataAccess =
                subNode.NodeType == ExpressionType.Constant && subNode.Type == functionInstanceArg.Type;
            if (isFunctionDataAccess)
                return true;
            if (subNode.NodeType == ExpressionType.MemberAccess)
                return IsDataAccess((MemberExpression)subNode);
            return false;
        }
    }

    public static bool SameMatchSignature(LambdaExpression expressionOne, LambdaExpression expressionTwo)
    {
        var isEqual = expressionOne != null && expressionTwo != null;
        if (isEqual is false) return false;
        if (expressionOne.ReturnType != expressionTwo.ReturnType)
            return false;
        if (expressionOne.Parameters.Count != expressionTwo.Parameters.Count)
            return false;
        var minParamsCount = Math.Min(expressionOne.Parameters.Count, expressionTwo.Parameters.Count);
        for (var i = 0; i < minParamsCount; i++)
            if (expressionOne.Parameters[i].Type != expressionTwo.Parameters[i].Type)
                return false;
        return true;
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
    public static MethodInfo GetInterfaceMethod(this MethodInfo method)
    {
        var type = method.DeclaringType;
        foreach (Type interf in type.GetInterfaces())
        {
            foreach (MethodInfo interfaceMethod in interf.GetMethods())
            {
                bool sameSiganture =
                    interfaceMethod.Name == method.Name &&
                    interfaceMethod.ReturnType == method.ReturnType &&
                    Enumerable.SequenceEqual(interfaceMethod.GetParameters().Select(x => x.ParameterType), method.GetParameters().Select(x => x.ParameterType));
                if (sameSiganture)
                    return interfaceMethod;
            }
        }
        return null;
    }

    public static MethodInfo GetMethodInfo(string AssemblyName, string ClassName, string MethodName, string MethodSignature)
    {
        MethodInfo _methodInfo = null;
        string assemblyPath = $"{AppContext.BaseDirectory}{AssemblyName}.dll";
        if (File.Exists(assemblyPath))
            if (AssemblyName != null && ClassName != null && MethodName != null)
            {
                _methodInfo = Assembly.LoadFrom(assemblyPath)
                    .GetType(ClassName)
                    ?.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == MethodName && MethodData.CalcSignature(x) == MethodSignature);
                return _methodInfo;
            }

        return _methodInfo;
    }

    //from:https://haacked.com/archive/2019/07/29/query-filter-by-interface/
    public static void AppendQueryFilter<T>(
                this EntityTypeBuilder<T> entityTypeBuilder, Expression<Func<T, bool>> expression) where T : class
    {
        var parameterType = Parameter(entityTypeBuilder.Metadata.ClrType);

        var expressionFilter = ReplacingExpressionVisitor.Replace(
            expression.Parameters.Single(), parameterType, expression.Body);

        if (entityTypeBuilder.Metadata.GetQueryFilter() != null)
        {
            var currentQueryFilter = entityTypeBuilder.Metadata.GetQueryFilter();
            var currentExpressionFilter = ReplacingExpressionVisitor.Replace(
                currentQueryFilter.Parameters.Single(), parameterType, currentQueryFilter.Body);
            expressionFilter = AndAlso(currentExpressionFilter, expressionFilter);
        }

        var lambdaExpression = Lambda(expressionFilter, parameterType);
        entityTypeBuilder.HasQueryFilter(lambdaExpression);
    }

    public static IEnumerable<T> Flatten<T>(
           this IEnumerable<T> e,
           Func<T, IEnumerable<T>> f) =>
               e.SelectMany(c => f(c).Flatten(f)).Concat(e);

    public static IEnumerable<T> Flatten<T>(this T value, Func<T, IEnumerable<T>> childrens)
    {
        foreach (var currentItem in childrens(value))
        {
            foreach (var currentChild in Flatten(currentItem, childrens))
            {
                yield return currentChild;
            }
        }
        yield return value;
    }

    public static void CascadeSet<T, Prop>(
        this T objectToSet,
        Expression<Func<IEnumerable<T>>> childs,
        Expression<Func<Prop>> prop,
        Prop value)
    {
        //IsDeleted = true;
        //foreach (var child in ChildWaits)
        //{
        //    child.CascadeSetDeleted();
        //}
    }
    public static IEnumerable<Prop> CascadeGet<T, Prop>(
       this T objectToSet,
       Expression<Func<IEnumerable<T>>> childs,
       Expression<Func<Prop>> prop,
       Prop value)
    {
        //IsDeleted = true;
        //foreach (var child in ChildWaits)
        //{
        //    child.CascadeSetDeleted();
        //}
        return null;
    }
}

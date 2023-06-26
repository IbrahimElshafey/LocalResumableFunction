﻿using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

public static class CoreExtensions
{
    //internal static IServiceProvider GetServiceProvider() => _ServiceProvider;
    public static void AddResumableFunctionsCore(this IServiceCollection services, IResumableFunctionsSettings settings)
    {


        // ReSharper disable once RedundantAssignment
        services.AddDbContext<FunctionDataContext>(optionsBuilder => optionsBuilder = settings.WaitsDbConfig);
        services.AddScoped<IMethodIdsRepo, MethodIdsRepo>();
        services.AddScoped<IWaitsRepo, WaitsRepo>();
        services.AddScoped<IServiceRepo, ServiceRepo>();
        services.AddScoped<IWaitTemplatesRepo, WaitTemplatesRepo>();
        services.AddScoped<IScanStateRepo, ScanStateRepo>();

        services.AddScoped<IFirstWaitProcessor, FirstWaitProcessor>();
        services.AddScoped<IRecycleBinService, RecycleBinService>();
        services.AddScoped<IReplayWaitProcessor, ReplayWaitProcessor>();
        services.AddScoped<IExpectedMatchesProcessor, ExpectedMatchesProcessor>();
        services.AddScoped<ICallProcessor, CallProcessor>();
        services.AddScoped<ICallPusher, CallPusher>();
        services.AddScoped<Scanner>();
        services.AddScoped<BackgroundJobExecutor>();



        services.AddSingleton<BinaryToObjectConverterAbstract, BinaryToObjectConverter>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<HangfireHttpClient>();
        services.AddSingleton(typeof(IResumableFunctionsSettings), settings);
        services.AddSingleton(settings.DistributedLockProvider);


        services.AddScoped<IUiService, UiService.UiService>();
        if (settings.HangfireConfig != null)
        {
            // ReSharper disable once RedundantAssignment
            services.AddHangfire(x => x = settings.HangfireConfig);
            services.AddSingleton<IBackgroundProcess, HangfireBackgroundProcess>();
            services.AddHangfireServer();
        }
        else
            services.AddSingleton<IBackgroundProcess, NoBackgroundProcess>();
    }

    public static void UseResumableFunctions(this IHost app)
    {
        GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(app.Services));
        StartScanProcess(app);
    }


    private static void StartScanProcess(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetService<IBackgroundProcess>();
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
        var attribute =
            method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) ??
            method.GetCustomAttribute(typeof(AsyncIteratorStateMachineAttribute));

        if (attribute != null) return true;

        var returnTypeIsTask =
            method is MethodInfo { ReturnType.IsGenericType: true } mi &&
            mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
        return returnTypeIsTask;
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
                    interfaceMethod.GetParameters().Select(x => x.ParameterType).SequenceEqual(method.GetParameters().Select(x => x.ParameterType));
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


    //https://www.newtonsoft.com/json/help/html/serializationguide.htm
    //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
    //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constants
    public static bool IsConstantType(this Type type)
    {
        var types = new[] { typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(short), typeof(ushort), typeof(string) };
        return types.Contains(type);
    }

    public static bool CanBeConstant(this object ob)=>
        ob != null && ob.GetType().IsConstantType();

    public static MethodInfo GetMethodInfo<T>(Expression<Func<T, object>> methodSelector) =>
        GetMethodInfoWithType(methodSelector).MethodInfo;
    public static (MethodInfo MethodInfo, Type OwnerType) GetMethodInfoWithType<T>(Expression<Func<T, object>> methodSelector)
    {
        MethodInfo mi = null;
        Type ownerType = null;
        var visitor = new GenericVisitor();
        visitor.OnVisitCall(VisitMethod);
        visitor.OnVisitConstant(VisitConstant);
        visitor.Visit(methodSelector);
        return (mi, ownerType);

        Expression VisitMethod(MethodCallExpression node)
        {
            if (IsInCurrentType(node.Method))
                mi = node.Method;
            return node;
        }
        Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is MethodInfo info && IsInCurrentType(info))
                mi = info;
            return node;
        }

        bool IsInCurrentType(MethodInfo methodInfo)
        {
            bool isExtension = methodInfo.IsDefined(typeof(ExtensionAttribute), true);
            if (isExtension)
            {
                var extensionOnType = methodInfo.GetParameters()[0].ParameterType;
                var canBeAppliedToCurrent = extensionOnType.IsAssignableFrom(typeof(T));
                if (canBeAppliedToCurrent)
                {
                    ownerType = extensionOnType;
                    return true;
                }

                return false;
            }

            bool inCurrentType = methodInfo.ReflectedType.IsAssignableFrom(typeof(T));
            if (inCurrentType)
                ownerType = methodInfo.ReflectedType;
            return inCurrentType;
        }
    }
}

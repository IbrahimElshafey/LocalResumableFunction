using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    public T LocalValue<T>(T value) => value;
    [IgnoreMember] internal MethodInfo CurrentResumableFunction { get; set; }

    public void Log(string message) => this.AddLog(message, LogType.Info, Helpers.StatusCodes.Custom);
    public void Warning(string message) => this.AddLog(message, LogType.Warning, Helpers.StatusCodes.Custom);
    public void Error(string message, Exception ex = null) => this.AddError(message, Helpers.StatusCodes.Custom, ex);

    [IgnoreMember] public int ErrorCounter { get; set; }

    [IgnoreMember][NotMapped] public List<LogRecord> Logs { get; set; } = new();

    private bool _dependenciesAreSet;
    internal void InitializeDependencies(IServiceProvider serviceProvider)
    {
        if (_dependenciesAreSet) return;
        var setDependenciesMi = GetType().GetMethod(
            "SetDependencies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (setDependenciesMi == null)
            return;

        var parameters = setDependenciesMi.GetParameters();
        var inputs = new object[parameters.Length];
        var matchSignature = setDependenciesMi.ReturnType == typeof(void) && parameters.Any();
        if (matchSignature)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                inputs[i] =
                    serviceProvider.GetService(parameters[i].ParameterType) ??
                    ActivatorUtilities.CreateInstance(serviceProvider, parameters[i].ParameterType);
            }
        }
        CallSetDependencies(inputs, setDependenciesMi, parameters);
        _dependenciesAreSet = true;
    }

    private void CallSetDependencies(object[] inputs, MethodInfo mi, ParameterInfo[] parameterTypes)
    {
        var instance = Expression.Parameter(GetType(), "instance");
        var depsParams = parameterTypes.Select(x => Expression.Parameter(x.ParameterType)).ToList();
        var parameters = new List<ParameterExpression>
        {
            instance
        };
        parameters.AddRange(depsParams);
        var call = Expression.Call(instance, mi, depsParams);
        var lambda = Expression.Lambda(call, parameters);
        var compiledFunction = lambda.CompileFast();
        var paramsAll = new List<object>(inputs.Length)
        {
            this
        };
        paramsAll.AddRange(inputs);
        compiledFunction.DynamicInvoke(paramsAll.ToArray());
    }
}
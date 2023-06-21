using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunction
{
    protected FunctionWait Wait(string name, Func<IAsyncEnumerable<Wait>> function)
    {
        var result = new FunctionWait
        {
            Name = name,
            IsNode = true,
            WaitType = WaitType.FunctionWait,
            FunctionInfo = function.Method,
            CurrentFunction = this,
        };
        return result;
    }

    protected WaitsGroup Wait(string name, params Func<IAsyncEnumerable<Wait>>[] subFunctions)
    {
        var result = new WaitsGroup
        {
            ChildWaits = new List<Wait>(new Wait[subFunctions.Length]),
            Name = name,
            IsNode = true,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        for (var index = 0; index < subFunctions.Length; index++)
        {
            var currentFunction = subFunctions[index];
            var currentFuncResult = Wait($"#{currentFunction.Method.Name}#", currentFunction);
            currentFuncResult.IsNode = false;
            currentFuncResult.ParentWait = result;
            result.ChildWaits[index] = currentFuncResult;
        }

        return result;
    }

    internal void InitializeDependencies(IServiceProvider serviceProvider)
    {
        //todo:should I create new scope??
        //void SetDependencies(dep1,dep2,....)
        var setDependenciesMi = GetType().GetMethod(
            "SetDependencies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (setDependenciesMi == null)
        {
            this.AddLog(
                "No instance method like `void SetDependencies(Interface dep1,...)` found that set your dependencies.",
                LogType.Warning);
            return;
        }

        var parameters = setDependenciesMi.GetParameters();
        var inputs = new object[parameters.Count()];
        bool matchSignature = setDependenciesMi.ReturnType == typeof(void) && parameters.Count() >= 1;
        if (matchSignature)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                inputs[i] =
                    serviceProvider.GetService(parameters[i].ParameterType) ??
                    ActivatorUtilities.CreateInstance(serviceProvider, parameters[i].ParameterType);
            }
        }
        else
        {
            this.AddLog(
               "No instance method like `void SetDependencies(Interface dep1,...)` found that set your dependencies.",
               LogType.Warning);
        }
        //setDependenciesMi.Invoke(this, inputs);
        CallSetDependencies(inputs, setDependenciesMi, parameters);
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
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.InOuts;
using System.Reflection;

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
        var setDepsMi = GetType().GetMethod(
            "SetDeps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setDepsMi == null)
            setDepsMi = GetType().GetMethod(
            "SetDependencies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setDepsMi == null)
            setDepsMi = GetType().GetMethod(
            "Dependencies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setDepsMi == null)
        {
            this.AddLog("No set dependencies method found that match the criteria.", LogType.Warning);
            return;
        }

        var parameters = setDepsMi.GetParameters();
        var inputs = new object[parameters.Count()];
        if (setDepsMi.ReturnType == typeof(void) &&
            parameters.Count() >= 1)
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
            this.AddLog("No set dependencies method found that match the criteria.", LogType.Warning);
        }
        setDepsMi.Invoke(this, inputs);
    }
}
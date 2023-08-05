using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    protected FunctionWait Wait(
        string name,
        Func<IAsyncEnumerable<Wait>> function,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        return new FunctionWait
        {
            Name = name,
            WaitType = WaitType.FunctionWait,
            FunctionInfo = function.Method,
            CurrentFunction = this,
            CallerName = callerName,
            InCodeLine = inCodeLine,
        };
    }

    protected WaitsGroup Wait(string name, params Func<IAsyncEnumerable<Wait>>[] subFunctions)
    {
        var functionGroup = new WaitsGroup
        {
            ChildWaits = new List<Wait>(new Wait[subFunctions.Length]),
            Name = name,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        for (var index = 0; index < subFunctions.Length; index++)
        {
            var currentFunction = subFunctions[index];
            var currentFuncResult = Wait($"#{currentFunction.Method.Name}#", currentFunction);
            currentFuncResult.ParentWait = functionGroup;
            functionGroup.ChildWaits[index] = currentFuncResult;
        }

        return functionGroup;
    }
}
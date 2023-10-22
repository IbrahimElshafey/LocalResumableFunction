using System.Runtime.CompilerServices;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    protected Wait Wait(
        string name,
        Func<IAsyncEnumerable<Wait>> function,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        //todo:validate attribute exist
        return new FunctionWaitEntity
        {
            Name = name,
            WaitType = WaitType.FunctionWait,
            FunctionInfo = function.Method,
            CurrentFunction = this,
            CallerName = callerName,
            InCodeLine = inCodeLine,
        }.ToWait();
    }

    protected WaitsGroup Wait(string name, params Func<IAsyncEnumerable<Wait>>[] subFunctions)
    {
        var functionGroup = new WaitsGroupEntity
        {   
            ChildWaits = new List<WaitEntity>(new WaitEntity[subFunctions.Length]),
            Name = name,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
        };
        for (var index = 0; index < subFunctions.Length; index++)
        {
            var currentFunction = subFunctions[index];
            var currentFuncResult = Wait($"#{currentFunction.Method.Name}#", currentFunction);
            currentFuncResult.WaitEntity.ParentWait = functionGroup;
            functionGroup.ChildWaits[index] = currentFuncResult.WaitEntity;
        }

        return functionGroup.ToWaitsGroup();
    }
}
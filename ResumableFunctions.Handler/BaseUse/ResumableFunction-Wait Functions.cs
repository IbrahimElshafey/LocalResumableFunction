using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ResumableFunctions.Handler;

public abstract partial class ResumableFunctionsContainer
{
    protected Wait Wait(
        string name,
        IAsyncEnumerable<Wait> function,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        var runner = function.GetAsyncEnumerator();
        var runnerName = function.GetAsyncEnumerator().GetType().Name;
        var methodName = Regex.Match(runnerName, "<(.+)>").Groups[1].Value;
        var functionInfo = GetType().GetMethod(methodName, CoreExtensions.DeclaredWithinTypeFlags());
        return new FunctionWaitEntity
        {
            Name = name ?? $"#{methodName}#",
            WaitType = WaitType.FunctionWait,
            FunctionInfo = functionInfo,
            CurrentFunction = this,
            CallerName = callerName,
            InCodeLine = inCodeLine,
            Runner = runner,
        }.ToWait();
    }

    protected WaitsGroup Wait(string name,
        IAsyncEnumerable<Wait>[] subFunctions,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
    {
        var functionGroup = new WaitsGroupEntity
        {
            ChildWaits = new List<WaitEntity>(new WaitEntity[subFunctions.Length]),
            Name = name,
            WaitType = WaitType.GroupWaitAll,
            CurrentFunction = this,
            CallerName = callerName,
            InCodeLine = inCodeLine,
        };
        for (var index = 0; index < subFunctions.Length; index++)
        {
            var currentFunction = subFunctions[index];
            var currentFuncResult = Wait(null, currentFunction);
            currentFuncResult.WaitEntity.ParentWait = functionGroup;
            functionGroup.ChildWaits[index] = currentFuncResult.WaitEntity;
        }
        return functionGroup.ToWaitsGroup();
    }
}
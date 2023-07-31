using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;

public class WaitsGroup : Wait
{

    public WaitsGroup()
    {
        WaitType = WaitType.GroupWaitAll;
    }

    public string GroupMatchFuncName { get; internal set; }
    public int CompletedCount => ChildWaits?.Count(x => x.Status == WaitStatus.Completed) ?? 0;

    public override bool IsCompleted()
    {
        var isFinished = false;
        switch (WaitType)
        {
            case WaitType.GroupWaitAll:
                isFinished = ChildWaits?.All(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitFirst:
                isFinished = ChildWaits?.Any(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitWithExpression when GroupMatchFuncName != null:
                var isCompleted =
                    MethodInvoker.CallGroupMatchFunc(CurrentFunction, GroupMatchFuncName, this);
                Status = isCompleted ? WaitStatus.Completed : Status;
                return isCompleted;

            case WaitType.GroupWaitWithExpression:
                isFinished = ChildWaits?.Any(x => x.Status == WaitStatus.Waiting) is false;
                break;
        }
        return isFinished;
    }




    public Wait When(Func<WaitsGroup, bool> groupMatchFilter)
    {
        var instanceType = CurrentFunction.GetType();
        if (groupMatchFilter.Method.DeclaringType != instanceType && groupMatchFilter.Method.DeclaringType.Name != "<>c")
            throw new Exception(
                $"For group wait [{Name}] the [{nameof(GroupMatchFuncName)}] must be a method in class " +
                $"[{instanceType.Name}] or inline lambda method.");
        var hasOverload = instanceType.GetMethods(Flags()).Count(x => x.Name == groupMatchFilter.Method.Name) > 1;
        if (hasOverload)
            throw new Exception(
                $"For group wait [{Name}] the [GroupMatchFunc:{groupMatchFilter.Method.Name}] must not be over-loaded.");

        WaitType = WaitType.GroupWaitWithExpression;
        GroupMatchFuncName = groupMatchFilter.Method.Name;
        return this;
    }

    public Wait All()
    {
        WaitType = WaitType.GroupWaitAll;
        return this;
    }

    public Wait First()
    {
        WaitType = WaitType.GroupWaitFirst;
        return this;
    }
}
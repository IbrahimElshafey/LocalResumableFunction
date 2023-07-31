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




    public Wait MatchIf(Func<WaitsGroup, bool> groupMatchFilter)
    {
        ValidateMethod(groupMatchFilter.Method, nameof(GroupMatchFuncName));
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
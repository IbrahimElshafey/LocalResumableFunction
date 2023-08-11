using ResumableFunctions.Handler.BaseUse;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class WaitsGroupEntity : WaitEntity
{

    internal WaitsGroupEntity()
    {
        WaitType = WaitType.GroupWaitAll;
    }

    public string GroupMatchFuncName { get; set; }

    internal override bool IsCompleted()
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
                var isCompleted = (bool)CallMethodByName(GroupMatchFuncName, ToWaitsGroup());
                Status = isCompleted ? WaitStatus.Completed : Status;
                return isCompleted;

            case WaitType.GroupWaitWithExpression:
                isFinished = ChildWaits?.Any(x => x.Status == WaitStatus.Waiting) is false;
                break;
        }
        return isFinished;
    }

    internal WaitsGroup ToWaitsGroup() => new WaitsGroup(this);
}
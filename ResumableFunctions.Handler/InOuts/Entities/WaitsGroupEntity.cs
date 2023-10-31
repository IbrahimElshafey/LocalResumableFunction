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
        var completed = false;
        switch (WaitType)
        {
            case WaitType.GroupWaitAll:
                completed = ChildWaits?.All(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitFirst:
                completed = ChildWaits?.Any(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitWithExpression when GroupMatchFuncName != null:
                //todo:[closure update] GroupWaitWithExpression
                var isCompleted = (bool)CallMethodByName(GroupMatchFuncName, ToWaitsGroup());
                Status = isCompleted ? WaitStatus.Completed : Status;
                return isCompleted;

            case WaitType.GroupWaitWithExpression:
                completed = ChildWaits?.Any(x => x.Status == WaitStatus.Waiting) is false;
                break;
        }
        //if (completed)
        //{
        //    Closure = ChildWaits.Last().Closure;
        //    Locals = ChildWaits.Last().Locals;
        //}
        return completed;
    }

    internal override void OnAddWait()
    {
        //Set Mutable Closure Id For Group
        if (ChildWaits.Any(x => x.MutableClosureId != null && CallerName == x.CallerName))
        {
            if (MutableClosureId == null)
                MutableClosureId = Guid.NewGuid();
            ChildWaits.ForEach(childWait =>
            {
                if (childWait.CallerName == CallerName)
                    childWait.MutableClosureId = MutableClosureId;
            });
        }
        base.OnAddWait();
    }

    internal WaitsGroup ToWaitsGroup() => new WaitsGroup(this);
}
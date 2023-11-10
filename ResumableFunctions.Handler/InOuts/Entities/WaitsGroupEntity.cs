using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Helpers;

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
                var isCompleted = (bool)CallMethodByName(GroupMatchFuncName, ToWaitsGroup());
                Status = isCompleted ? WaitStatus.Completed : Status;
                return isCompleted;

            case WaitType.GroupWaitWithExpression:
                completed = ChildWaits?.Any(x => x.Status == WaitStatus.Waiting) is false;
                break;
        }
        return completed;
    }

    internal override void OnAddWait()
    {
        //todo: different closures for children 
        //wait group(privateMwthod1(),privateMwthod2,...)
        var childHasClosure = ChildWaits.Any(x => x.RuntimeClosureId != null && CallerName == x.CallerName);
        if (childHasClosure)
        {
            if (RuntimeClosureId == null)
                RuntimeClosureId = Guid.NewGuid();
            ChildWaits.ForEach(childWait =>
            {
                if (childWait.CallerName == CallerName)
                    childWait.RuntimeClosureId = RuntimeClosureId;
            });
        }
        ActionOnChildrenTree(w => w.IsRoot = w.ParentWait == null && w.ParentWaitId == null);
        ValidateMethodNameDuplicationIfFirst();
        base.OnAddWait();
    }
    void ValidateMethodNameDuplicationIfFirst()
    {
        if (!(IsFirst && IsRoot)) return;

        var groups =
            GetTreeItems().
            Where(x => x is MethodWaitEntity).
            GroupBy(x => x.Name);

        foreach (var g in groups)
        {
            if (g.Count() > 1)
            {

                FunctionState.AddLog(
                    $"The group wait named [{Name}] contains a duplicated method wait named [{g.Key}].",
                    LogType.Error, StatusCodes.WaitValidation);
            }
        }

    }
    internal override bool ValidateWaitRequest()
    {
        if (ChildWaits == null || !ChildWaits.Any())
        {
            FunctionState.AddLog(
                $"The group wait named [{Name}] does not have childern, You must add one wait at least.",
                LogType.Error,
                StatusCodes.WaitValidation);
        }
        if (ChildWaits.Any(x => x == null))
        {
            FunctionState.AddLog(
                $"The group wait named [{Name}] contains wait that has null value.",
                LogType.Error,
                StatusCodes.WaitValidation);
        }
        return base.ValidateWaitRequest();
    }
    internal WaitsGroup ToWaitsGroup() => new WaitsGroup(this);
}
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts.Entities;

public sealed class FunctionWaitEntity : WaitEntity
{
    internal FunctionWaitEntity()
    {

    }
    [NotMapped]
    public WaitEntity FirstWait { get; set; }

    [NotMapped] public MethodInfo FunctionInfo { get; set; }

    internal override bool IsCompleted() => ChildWaits.Any(x => x.Status == WaitStatus.Waiting) is false;

    internal override void OnAddWait()
    {
        IsRoot = ParentWait == null && ParentWaitId == null;
        base.OnAddWait();
    }
}

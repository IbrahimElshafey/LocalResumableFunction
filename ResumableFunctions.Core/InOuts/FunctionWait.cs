using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Core.InOuts;

public sealed class FunctionWait : Wait
{
    [NotMapped]
    public Wait FirstWait { get; internal set; }

    [NotMapped] public MethodInfo FunctionInfo { get; internal set; }

    public override bool IsFinished() => ChildWaits.Any(x => x.Status == WaitStatus.Waiting) is false;

}
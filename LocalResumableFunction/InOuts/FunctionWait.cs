using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace LocalResumableFunction.InOuts;

public sealed class FunctionWait : Wait
{
    [NotMapped]
    public Wait FirstWait { get; internal set; }

    [NotMapped] public MethodInfo FunctionInfo { get; internal set; }

    public bool IsFinished() => this.ChildWaits.Any(x => x.Status == WaitStatus.Waiting) is false;

}
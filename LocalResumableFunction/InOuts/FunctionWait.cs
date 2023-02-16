using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace LocalResumableFunction.InOuts;

public sealed class FunctionWait : Wait
{
    public ManyFunctionsWait ParentFunctionGroup { get; internal set; }
    public int? ParentFunctionGroupId { get; internal set; }
    public Wait FirstWait { get; internal set; }
    public int FirstWaitId { get; internal set; }

    [NotMapped]
    public MethodInfo FunctionInfo { get; internal set; }
}
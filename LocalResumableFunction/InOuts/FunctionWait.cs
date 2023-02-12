using System.ComponentModel.DataAnnotations.Schema;

namespace LocalResumableFunction.InOuts
{
    public sealed class FunctionWait : Wait
    {
        public ManyFunctionsWait ParentFunctionGroup { get; internal set; }
        public int? ParentFunctionGroupId { get; internal set; }
        public Wait FirstWait { get; internal set; }
        public int FirstWaitId { get; internal set; }
    }
}
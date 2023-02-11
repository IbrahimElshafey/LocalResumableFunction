using System.ComponentModel.DataAnnotations.Schema;

namespace LocalResumableFunction.InOuts
{
    public sealed class FunctionWait : Wait
    {
        public FunctionWait() : base()
        {

        }
        public FunctionWait(string name, Func<IAsyncEnumerable<Wait>> function)
        {
            Name = name;
            FunctionName = function.Method.Name;
        }
        public ManyFunctionsWait ParentFunctionGroup { get; internal set; }
        public int? ParentFunctionGroupId { get; internal set; }
        public Wait CurrentWait { get; internal set; }
        public string FunctionName { get; internal set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace LocalResumableFunction.InOuts
{
    public class FunctionRuntimeInfo
    {
        [Key]
        public int Id { get; internal set; }

        /// <summary>
        /// The class that contains the resumable functions
        /// </summary>
        public Type InitiatedByClassType { get; internal set; }

        //has the state serialized
        public object? FunctionState { get; internal set; }

        public List<Wait> Waits { get; internal set; } = new();
    }
}
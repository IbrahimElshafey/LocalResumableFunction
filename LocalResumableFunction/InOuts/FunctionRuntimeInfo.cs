using System.ComponentModel.DataAnnotations;

namespace LocalResumableFunction.InOuts
{
    public class FunctionRuntimeInfo
    {
        [Key]
        public int FunctionId { get; internal set; }

        /// <summary>
        /// The class that contians the resumable functions
        /// </summary>
        public Type InitiatedByClassType { get; internal set; }

        //has the state serialzed
        public object FunctionState { get; internal set; }

        public List<Wait> FunctionWaits { get; internal set; } = new List<Wait>();
    }
}
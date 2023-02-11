using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace LocalResumableFunction.InOuts
{
    public class ResumableFunctionState
    {
        [Key]
        public int Id { get; internal set; }
        
        [NotMapped]
        public MethodInfo ResumableFunctionMethodInfo { get; set; }

        public MethodIdentifier ResumableFunctionIdentifier { get; set; }
        public int ResumableFunctionIdentifierId { get; set; }

        //class instance that contain the resumable function
        public object? StateObject { get; internal set; }

        public List<Wait> Waits { get; internal set; } = new();
    }
}
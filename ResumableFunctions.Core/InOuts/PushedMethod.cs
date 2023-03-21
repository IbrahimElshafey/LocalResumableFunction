using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Core.InOuts;

public class PushedMethod
{
    public int Id { get; set; }
    [NotMapped]
    public MethodData MethodData { get; internal set; }
    public int MethodId { get; internal set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }
}
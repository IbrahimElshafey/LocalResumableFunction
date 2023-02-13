using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Newtonsoft.Json;

namespace LocalResumableFunction.InOuts;

public class ResumableFunctionState
{
    [Key]
    [JsonIgnore]
    public int Id { get; internal set; }

    //class instance that contain the resumable function
    public object? StateObject { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();
    public MethodIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
}
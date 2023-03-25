using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ResumableFunctions.Core.InOuts;

public class ResumableFunctionState
{
    [Key] public int Id { get; internal set; }
    public FunctionStatus Status { get; internal set; } = FunctionStatus.New;
    public string StatusMessage { get; internal set; }

    //class instance that contain the resumable function
    public object StateObject { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();
    public MethodIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
    public bool IsLocked { get; set; }

}


using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;

public class ResumableFunctionState : EntityWithLogs, IEntityWithUpdate, IEntityWithDelete
{

    /// <summary>
    /// Serailized class instance that contain the resumable function class
    /// </summary>
    public object StateObject { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();



    public ResumableFunctionIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
    public FunctionStatus Status { get; set; }//todo:reset before scan
    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public bool IsDeleted { get; internal set; }
}

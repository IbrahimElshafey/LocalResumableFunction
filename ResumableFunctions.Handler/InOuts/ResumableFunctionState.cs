
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;

public class ResumableFunctionState : IObjectWithLog, IEntityWithUpdate, IEntityWithDelete
{
    [JsonIgnore]
    public int ErrorCounter { get; set; }

    [JsonIgnore]
    [NotMapped]
    public List<LogRecord> Logs { get; } = new();
    public int Id { get; internal set; }
    public int ServiceId { get; internal set; }
    public string UserDefinedId { get; internal set; }
    public DateTime Created { get; internal set; }
    /// <summary>
    /// Serailized class instance that contain the resumable function instance data
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

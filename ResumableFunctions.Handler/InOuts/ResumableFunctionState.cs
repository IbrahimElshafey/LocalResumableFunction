
using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;
public class ResumableFunctionState : IObjectWithLog, IEntityWithUpdate, IEntityWithDelete, IOnSaveEntity
{
    [IgnoreMember]
    public int ErrorCounter { get; set; }

    [IgnoreMember]
    [NotMapped]
    public List<LogRecord> Logs { get; set; } = new();
    public int Id { get; internal set; }
    public int? ServiceId { get; set; }
    public string UserDefinedId { get; internal set; }
    public DateTime Created { get; internal set; }
    /// <summary>
    /// Serialized class instance that contain the resumable function instance data
    /// </summary>
    [NotMapped]
    public object StateObject { get; internal set; }
    public byte[] StateObjectValue { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();


    public ResumableFunctionIdentifier ResumableFunctionIdentifier { get; internal set; }
    public int ResumableFunctionIdentifierId { get; internal set; }
    public FunctionStatus Status { get; internal set; }
    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void OnSave()
    {
        var converter = new BinaryToObjectConverter();
        StateObjectValue = converter.ConvertToBinary(StateObject);
    }

    public void LoadUnmappedProps(Type stateObjectType)
    {
        var converter = new BinaryToObjectConverter();
        StateObject =
            stateObjectType != null ?
                converter.ConvertToObject(StateObjectValue, stateObjectType) :
                null;
    }
}

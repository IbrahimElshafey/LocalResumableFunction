
using Newtonsoft.Json;
using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ResumableFunctions.Handler.InOuts;

public class ResumableFunctionState : IObjectWithLog, IEntityWithUpdate, IEntityWithDelete, IEntityInService, IOnSaveEntity,ILoadUnMapped
{
    [JsonIgnore]
    public int ErrorCounter { get; set; }

    [JsonIgnore]
    [NotMapped]
    public List<LogRecord> Logs { get; } = new();
    public int Id { get; internal set; }
    public int? ServiceId { get; set; }
    public string UserDefinedId { get; internal set; }
    public DateTime Created { get; internal set; }
    /// <summary>
    /// Serailized class instance that contain the resumable function instance data
    /// </summary>
    [NotMapped]
    public object StateObjectn { get; internal set; }
    public byte[] StateObjectValue { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();


    public ResumableFunctionIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
    public FunctionStatus Status { get; set; }//todo:reset before scan
    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void OnSave()
    {
        var converter = new NewtonsoftBinaryToObjectConverter();
        StateObjectValue = converter.ConvertToBinary(StateObjectn);
    }

    public void LoadUnmappedProps(params object[] args)
    {
        var converter = new NewtonsoftBinaryToObjectConverter();
        if (args != null && args[0] != null)
            StateObjectn = converter.ConvertToObject(StateObjectValue, (Type)args[0]);
        else
            StateObjectn = converter.ConvertToObject(StateObjectValue);
    }
}

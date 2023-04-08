
namespace ResumableFunctions.Handler.InOuts;

public class ResumableFunctionState
{
    public ResumableFunctionState()
    {

    }
    public int Id { get; internal set; }



    /// <summary>
    /// Serailized class instance that contain the resumable function class
    /// </summary>
    public object StateObject { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();
    public List<FunctionStateLogRecord> LogRecords { get; internal set; } = new();
    public ResumableFunctionIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
    public bool IsLocked { get; set; }
    public FunctionStatus Status { get; set; }

    public void LogStatus(FunctionStatus status, string statusMessage)
    {
        Status = status;
        LogRecords.Add(new FunctionStateLogRecord { Status = status, StatusMessage = statusMessage });
    }

}

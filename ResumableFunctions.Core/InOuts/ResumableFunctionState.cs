﻿
namespace ResumableFunctions.Core.InOuts;

public class ResumableFunctionState
{
    public int Id { get; internal set; }



    /// <summary>
    /// Serailized class instance that contain the resumable function class
    /// </summary>
    public object StateObject { get; internal set; }

    public List<Wait> Waits { get; internal set; } = new();
    public List<FunctionStateLogRecord> LogRecords { get; internal set; } = new();
    public MethodIdentifier ResumableFunctionIdentifier { get; set; }
    public int ResumableFunctionIdentifierId { get; set; }
    public bool IsLocked { get; set; }
    public bool IsCompleted { get; set; }

    public void LogStatus(FunctionStatus status, string statusMessage)
    {
        LogRecords.Add(new FunctionStateLogRecord { Status = status, StatusMessage = statusMessage });

        if (status == FunctionStatus.Completed) 
            IsCompleted = true;
    }

}

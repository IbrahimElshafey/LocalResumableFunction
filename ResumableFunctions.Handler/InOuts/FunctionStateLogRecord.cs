
namespace ResumableFunctions.Handler.InOuts;

public class FunctionStateLogRecord:IEntity
{
    public int Id { get; internal set; }
    public int FunctionStateId { get; internal set; }
    public ResumableFunctionState FunctionState { get; internal set; }
    public LogStatus Status { get; internal set; } = LogStatus.New;
    public string StatusMessage { get; internal set; }
    public DateTime Created { get; internal set; }
}

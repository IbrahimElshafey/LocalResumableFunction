
namespace ResumableFunctions.Core.InOuts;

public class FunctionStateLogRecord
{
    public int Id { get; internal set; }
    public int FunctionStateId { get; internal set; }
    public ResumableFunctionState FunctionState { get; internal set; }
    public FunctionStatus Status { get; internal set; } = FunctionStatus.New;
    public string StatusMessage { get; internal set; }
}

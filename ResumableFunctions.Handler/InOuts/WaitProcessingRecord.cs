namespace ResumableFunctions.Handler.InOuts;

public class WaitProcessingRecord : IEntityWithUpdate
{
    public int Id { get; internal set; }
    public int PushedCallId { get; internal set; }
    public int WaitId { get; internal set; }
    public int? ServiceId { get; set; }
    public int FunctionId { get; internal set; }
    public int StateId { get; internal set; }
    public int TemplateId { get; internal set; }
    public MatchStatus MatchStatus { get; internal set; } = MatchStatus.ExpectedMatch;
    public InstanceUpdateStatus InstanceUpdateStatus { get; internal set; } = InstanceUpdateStatus.NotUpdatedYet;
    public ExecutionStatus ExecutionStatus { get; internal set; } = ExecutionStatus.NotStartedYet;
    public DateTime Created { get; internal set; }

    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public override bool Equals(object obj)
    {
        if (obj is WaitProcessingRecord wfc)
        {
            return wfc.WaitId == WaitId && wfc.FunctionId == FunctionId && wfc.StateId == StateId;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return $"{WaitId}-{FunctionId}-{StateId}".GetHashCode();
    }
}

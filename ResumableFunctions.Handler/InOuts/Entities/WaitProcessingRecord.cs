namespace ResumableFunctions.Handler.InOuts.Entities;

public class WaitProcessingRecord : IEntity<long>, IEntityWithUpdate
{
    public long Id { get; set; }
    public long PushedCallId { get; set; }
    public long WaitId { get; set; }
    public int? ServiceId { get; set; }
    public int FunctionId { get; set; }
    public int StateId { get; set; }
    public int TemplateId { get; set; }
    public MatchStatus MatchStatus { get; set; } = MatchStatus.ExpectedMatch;
    public ExecutionStatus AfterMatchActionStatus { get; set; } = ExecutionStatus.NotStartedYet;
    public ExecutionStatus ExecutionStatus { get; set; } = ExecutionStatus.NotStartedYet;
    public DateTime Created { get; set; }

    public DateTime Modified { get; set; }
    public string ConcurrencyToken { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is WaitProcessingRecord waitProcessingRecord)
        {
            return
                waitProcessingRecord.WaitId == WaitId &&
                waitProcessingRecord.FunctionId == FunctionId &&
                waitProcessingRecord.StateId == StateId;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return $"{WaitId}-{FunctionId}-{StateId}".GetHashCode();
    }
}

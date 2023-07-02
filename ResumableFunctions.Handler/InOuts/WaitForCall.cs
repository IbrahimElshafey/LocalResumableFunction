namespace ResumableFunctions.Handler.InOuts;

public class WaitForCall : IEntityWithUpdate
{
    public long Id { get; internal set; }
    public PushedCall PushedCall { get; internal set; }
    public long PushedCallId { get; internal set; }
    public long WaitId { get; internal set; }
    public long? ServiceId { get; set; }
    public long FunctionId { get; internal set; }
    public long StateId { get; internal set; }
    public MatchStatus MatchStatus { get; internal set; } = MatchStatus.ExpectedMatch;
    public InstanceUpdateStatus InstanceUpdateStatus { get; internal set; } = InstanceUpdateStatus.NotUpdatedYet;
    public ExecutionStatus ExecutionStatus { get; internal set; } = ExecutionStatus.NotStartedYet;
    public DateTime Created { get; internal set; }

    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }
}

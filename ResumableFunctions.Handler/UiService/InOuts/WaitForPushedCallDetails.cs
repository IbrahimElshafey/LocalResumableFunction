using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record WaitForPushedCallDetails(
        string Name,
        WaitStatus Status,
        string FunctionName,
        int InstanceId,
        string MatchExpression,
        string SetDataExpression,
        DateTime Created,
        string MandatoryPart,
        string MandatoryPartExpression,
        MatchStatus MatchStatus,
        InstanceUpdateStatus InstanceUpdateStatus,
        ExecutionStatus ExecutionStatus
        );
}

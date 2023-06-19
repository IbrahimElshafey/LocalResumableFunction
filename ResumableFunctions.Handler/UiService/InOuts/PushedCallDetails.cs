using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record PushedCallDetails
    {
        public string InputOutput { get; }
        public string MethodUrn { get; }
        public List<WaitForPushedCallDetails> Waits { get; }

        public PushedCallDetails(string inputOutput, string methodUrn, List<WaitForPushedCallDetails> waits)
        {
            InputOutput = inputOutput;
            MethodUrn = methodUrn;
            Waits = waits;
        }
    }

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

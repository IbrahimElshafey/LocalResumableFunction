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
        string Name,//wait
        WaitStatus Status,
        string FunctionName,//wait.RequestedByFunc.Name
        int InstanceId,
        string MatchExpression,//wait.template.match
        string SetDataExpression,
        DateTime Created,
        string MandatoryPart,
        string MandatoryPartExpression,//wait.template.man
        MatchStatus MatchStatus,//call
        InstanceUpdateStatus InstanceUpdateStatus,
        ExecutionStatus ExecutionStatus
        );
}

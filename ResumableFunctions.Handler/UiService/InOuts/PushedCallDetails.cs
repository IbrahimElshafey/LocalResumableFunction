namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class PushedCallDetails
    {
        public string InputOutput { get; }
        public string MethodUrn { get; }
        public List<MethodWaitDetails> Waits { get; }

        public PushedCallDetails(string inputOutput, string methodUrn, List<MethodWaitDetails> waits)
        {
            InputOutput = inputOutput;
            MethodUrn = methodUrn;
            Waits = waits;
        }
    }
}

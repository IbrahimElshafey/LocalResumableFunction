namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class PushedCallDetails
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
}

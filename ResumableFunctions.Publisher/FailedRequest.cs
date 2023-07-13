namespace ResumableFunctions.Publisher
{
    public class FailedRequest
    {
        public int Id { get; internal set; }
        public string ActionUrl { get; internal set; }
        public byte[] Body { get; internal set; }
        public DateTime Created { get; internal set; }
        public int AttemptsCount { get; internal set; } = 1;
        public DateTime LastAttemptDate { get; internal set; }
    }
}
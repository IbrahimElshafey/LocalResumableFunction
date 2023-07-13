namespace ResumableFunctions.Publisher
{
    public class FailedRequest
    {
        public Guid Id { get;  set; }
        public string ActionUrl { get;  set; }
        public byte[] Body { get;  set; }
        public DateTime Created { get;  set; }
        public int AttemptsCount { get;  set; } = 1;
        public DateTime LastAttemptDate { get;  set; }
    }
}
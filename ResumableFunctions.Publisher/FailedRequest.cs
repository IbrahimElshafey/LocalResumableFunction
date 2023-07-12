namespace ResumableFunctions.Publisher
{
    public class FailedRequest
    {
        public int Id { get; set; }
        public string ActionUrl { get; set; }
        public byte[] Body { get; set; }
        public DateTime Created { get; set; }
        public int AttemptsCount { get; set; }
    }
}
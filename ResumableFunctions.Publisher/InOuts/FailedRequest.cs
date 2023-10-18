using System;

namespace ResumableFunctions.Publisher.InOuts
{
    public class FailedRequest
    {
        public Guid Key { get;  set; }
        public string ActionUrl { get;  set; }
        public byte[] Body { get;  set; }
        public DateTime Created { get;  set; }
        public int AttemptsCount { get;  set; } = 1;
        public DateTime LastAttemptDate { get;  set; }
    }
}
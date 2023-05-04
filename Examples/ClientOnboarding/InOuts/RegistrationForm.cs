namespace ClientOnboarding.InOuts
{
    public class RegistrationForm
    {
        public int UserId { get;  set; }
        public string FormData { get;  set; }
    }

    public class RegistrationResult
    {
        public int FormId { get; set; }
    }

    public class TaskId
    {
        public int Id { get; set; }
    }

    public class OwnerApproveClientInput
    {
        public bool Decision { get; set; }
        public int TaskId { get; set; }
    }
    public class OwnerApproveClientResult
    {
        public int OwnerApprovalId { get; set; }
    }

    public class ClientMeetingId
    {
        public int MeetingId { get; set; }
        public int UserId { get; set; }
    }
    public class MeetingResult
    {
        public int MeetingId { get; set; }
        public int MeetingResultId { get; set; }
        public bool ClientRejectTheDeal { get; set; }
        public bool ClientAcceptTheDeal { get; set; }
    }
}

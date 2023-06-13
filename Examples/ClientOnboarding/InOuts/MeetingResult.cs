namespace ClientOnboarding.InOuts;

public class MeetingResult
{
    public int MeetingId { get; set; }
    public int MeetingResultId { get; set; }
    public bool ClientRejectTheDeal { get; set; }
    public bool ClientAcceptTheDeal { get; set; }
}
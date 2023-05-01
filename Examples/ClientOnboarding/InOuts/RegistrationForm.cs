namespace ClientOnboarding.InOuts
{
    public class RegistrationForm
    {
        public int Id { get; internal set; }
        public int UserId { get;  set; }
        public string FormData { get;  set; }
    }

    public class RegistrationResult
    {
        public int FormId { get; set; }
    }
}

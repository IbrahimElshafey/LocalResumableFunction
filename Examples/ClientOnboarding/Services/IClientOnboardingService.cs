using ClientOnboarding.InOuts;

namespace ClientOnboarding.Services
{
    public interface IClientOnboardingService
    {
        TaskId AskOwnerToApproveClient(int registrationFormId);
        RegistrationResult ClientFillsForm(RegistrationForm registrationForm);
        void InformUserAboutRejection(int userId);
        OwnerApproveClientResult OwnerApproveClient(OwnerApproveClientInput ownerApproveClientInput);
        MeetingResult SendMeetingResult(int meetingId);
        void SendWelcomePackage(int userId);
        ClientMeetingId SetupInitalMeetingAndAgenda(int userId);
    }
}
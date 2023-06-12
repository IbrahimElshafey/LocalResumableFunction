using ClientOnboarding.InOuts;
using ResumableFunctions.Handler.Attributes;

namespace ClientOnboarding.Services
{
    internal class ClientOnboardingService : IClientOnboardingService
    {

        [PushCall("ClientOnboardingService.ClientFillsForm")]
        public RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
        {
            return new RegistrationResult
            {
                FormId = Random.Shared.Next()
            };
        }

        public TaskId AskOwnerToApproveClient(int registrationFormId)
        {
            return new TaskId { Id = registrationFormId };
        }

        [PushCall("ClientOnboardingService.OwnerApproveClient")]
        public OwnerApproveClientResult OwnerApproveClient(OwnerApproveClientInput ownerApproveClientInput)
        {
            return new OwnerApproveClientResult
            {
                OwnerApprovalId = Random.Shared.Next(),
            };
        }

        public void InformUserAboutRejection(int userId)
        {
            Console.WriteLine($"InformUserAboutRejection called userId: {userId}");
        }

        public void SendWelcomePackage(int userId)
        {
            Console.WriteLine($"SendWelcomePackage called userId: {userId}");
        }

        public ClientMeetingId SetupInitalMeetingAndAgenda(int userId)
        {
            Console.WriteLine($"SetupInitalMeetingAndAgenda called userId: {userId}");
            return new ClientMeetingId
            {
                MeetingId = Random.Shared.Next(),
                UserId = userId
            };
        }

        [PushCall("ClientOnboardingService.SendMeetingResult")]
        public MeetingResult SendMeetingResult(int meetingId)
        {
            var id = Random.Shared.Next();
            return new MeetingResult
            {
                MeetingId = meetingId,
                MeetingResultId = id,
                ClientAcceptTheDeal = id % 2 == 1,
                ClientRejectTheDeal = id % 2 == 0,
            };
        }
    }
}

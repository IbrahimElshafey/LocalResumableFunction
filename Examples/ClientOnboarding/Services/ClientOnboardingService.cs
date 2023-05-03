using ClientOnboarding.InOuts;
using ResumableFunctions.Handler.Attributes;

namespace ClientOnboarding.Services
{
    public class ClientOnboardingService
    {
        public ClientOnboardingService()
        {

        }

        [WaitMethod("ClientOnboardingService.ClientFillsForm")]
        internal RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
        {
            return new RegistrationResult
            {
                FormId = Random.Shared.Next()
            };
        }

        internal TaskId AskOwnerToApproveClient(int registrationFormId)
        {
            return new TaskId { Id = registrationFormId };
        }

        [WaitMethod("ClientOnboardingService.OwnerApproveClient")]
        internal OwnerApproveClientResult OwnerApproveClient(int taskId)
        {
            return new OwnerApproveClientResult
            {
                Decision = Random.Shared.Next() % 2 == 1,
                RegistrationFormId = taskId,
                TaskId = taskId
            };
        }

        internal void InformUserAboutRejection(int userId)
        {
            Console.WriteLine($"InformUserAboutRejection called userId: {userId}");
        }

        internal void SendWelcomePackage(int userId)
        {
            Console.WriteLine($"SendWelcomePackage called userId: {userId}");
        }

        internal ClientMeetingId SetupInitalMeetingAndAgenda(int userId)
        {
            Console.WriteLine($"SetupInitalMeetingAndAgenda called userId: {userId}");
            return new ClientMeetingId
            {
                MeetingId = Random.Shared.Next(),
                UserId = userId
            };
        }

        [WaitMethod("ClientOnboardingService.SendMeetingResult")]
        internal MeetingResult SendMeetingResult(int meetingId)
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

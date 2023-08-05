using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ClientOnboarding.Workflow
{
    public class ClientOnboardingWorkflowPublic : ResumableFunctionsContainer
    {
        private IClientOnboardingService? _service;

        public void SetDependencies(IClientOnboardingService service)
        {
            _service = service;
        }

        public int FormId { get; set; }
        public int UserId { get; set; }
        public int OwnerTaskId { get; set; }
        public int ClientMeetingId { get; set; }
        public bool OwnerDecision { get; set; }

        [ResumableFunctionEntryPoint("ClientOnboardingWorkflowPublic.Start")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            yield return WaitClientFillForm();

            yield return AskOwnerToApprove();

            if (OwnerDecision is false)
                _service.InformUserAboutRejection(UserId);
            
            else if (OwnerDecision is true)
            {
                _service.SendWelcomePackage(UserId);
                yield return WaitMeetingResult();
            }

            Console.WriteLine("User Registration Done");
        }

        private Wait WaitMeetingResult()
        {
            ClientMeetingId = _service.SetupInitalMeetingAndAgenda(UserId).MeetingId;
            return
                Wait<int, MeetingResult>(_service.SendMeetingResult, "Wait Meeting Result")
               .MatchIf((meetingId, meetingResult) => meetingId == ClientMeetingId)
               .AfterMatch((meetingId, meetingResult) => Console.WriteLine(ClientMeetingId));
        }

        private Wait AskOwnerToApprove()
        {
            OwnerTaskId = _service.AskOwnerToApproveClient(FormId).Id;
            return
                Wait<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, "Wait Owner Approve Client")
                .MatchIf((approveClientInput, approveResult) => approveClientInput.TaskId == OwnerTaskId)
                .AfterMatch((approveClientInput, approveResult) =>
                {
                    OwnerDecision = approveClientInput.Decision;
                });
        }

        private Wait WaitClientFillForm()
        {
            return
                Wait<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, "Wait User Registration")
                .MatchIf((regForm, regResult) => regResult.FormId > 0)
                .AfterMatch((regForm, regResult) =>
                {
                    FormId = regResult.FormId;
                    UserId = regForm.UserId;
                });
        }
    }
}

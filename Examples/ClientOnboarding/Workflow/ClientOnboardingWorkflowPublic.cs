using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
using static ClientOnboarding.Workflow.ClientOnboardingWorkflowPrivate;

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
                WaitMethod<int, MeetingResult>(_service.SendMeetingResult, WaitNames.MeetingResult)
               .MatchIf((meetingId, meetingResult) => meetingId == ClientMeetingId)
               .AfterMatch((meetingId, meetingResult) => Console.WriteLine(ClientMeetingId));
        }

        private Wait AskOwnerToApprove()
        {
            OwnerTaskId = _service.AskOwnerToApproveClient(FormId).Id;
            return
                WaitMethod<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, WaitNames.OwnerApprove)
                .MatchIf((approveClientInput, approveResult) => approveClientInput.TaskId == OwnerTaskId)
                .AfterMatch((approveClientInput, approveResult) => OwnerDecision = approveClientInput.Decision);
        }

        private Wait WaitClientFillForm()
        {
            return
                WaitMethod<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, WaitNames.UserRegistration)
                .MatchIf((regForm, regResult) => regResult.FormId > 0)
                .AfterMatch((regForm, regResult) =>
                {
                    FormId = regResult.FormId;
                    UserId = regForm.UserId;
                });
        }
    }
}

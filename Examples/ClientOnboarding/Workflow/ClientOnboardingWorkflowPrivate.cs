using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace ClientOnboarding.Workflow
{
    public class ClientOnboardingWorkflowPrivate : ResumableFunctionsContainer
    {
        private IClientOnboardingService? _service;
        public int FormId { get; set; }
        public void SetDependencies(IClientOnboardingService service)
        {
            _service = service;
        }


        [ResumableFunctionEntryPoint("ClientOnboardingWorkflowPrivate.Start")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            var userId = -1;
            yield return
                Wait<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, "Wait User Registration")
                .MatchIf((regForm, regResult) => regResult.FormId > 0)
                .AfterMatch((regForm, regResult) =>
                {
                    FormId = regResult.FormId;
                    userId = regForm.UserId;
                });

            var ownerTaskId = _service.AskOwnerToApproveClient(FormId).Id;
            var ownerDecision = false;
            yield return
                Wait<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, "Wait Owner Approve Client")
                .MatchIf((approveClientInput, _) => approveClientInput.TaskId == ownerTaskId)
                .AfterMatch((approveClientInput, _) => ownerDecision = approveClientInput.Decision);
            if (ownerDecision is false)
            {
                _service.InformUserAboutRejection(userId);
            }
            else if (ownerDecision)
            {
                _service.SendWelcomePackage(userId);
                var clientMeetingId = _service.SetupInitalMeetingAndAgenda(userId).MeetingId;

                yield return
                    Wait<int, MeetingResult>(_service.SendMeetingResult, "Wait Meeting Result")
                   .AfterMatch((_, _) =>
                   {
                       Console.WriteLine("Closure level 2 and public method");
                       Console.WriteLine(clientMeetingId);
                       Console.WriteLine(userId);
                       FormId += 1000;
                   })
                   .MatchIf((meetingId, _) => meetingId == clientMeetingId);

                Console.WriteLine(clientMeetingId);
            }

            Console.WriteLine("User Registration Done");
        }
    }
}

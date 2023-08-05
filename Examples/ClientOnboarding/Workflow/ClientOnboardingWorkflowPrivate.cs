using System.Linq.Expressions;
using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using MessagePack;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ClientOnboarding.Workflow
{
    public class ClientOnboardingWorkflowPrivate : ResumableFunctionsContainer
    {
        private IClientOnboardingService? _service;

        public void SetDependencies(IClientOnboardingService service)
        {
            _service = service;
        }


        [ResumableFunctionEntryPoint("ClientOnboardingWorkflowPrivate.Start")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            var formId = -1;
            var userId = -1;
            yield return
                Wait<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, "Wait User Registration")
                .MatchIf((regForm, regResult) => regResult.FormId > 0)
                .AfterMatch((regForm, regResult) =>
                {
                    formId = regResult.FormId;
                    userId = regForm.UserId;
                });

            var ownerTaskId = _service.AskOwnerToApproveClient(formId).Id;
            var ownerDecision = false;
            yield return
                Wait<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, "Wait Owner Approve Client")
                .MatchIf((approveClientInput, approveResult) => approveClientInput.TaskId == ownerTaskId)
                .AfterMatch((approveClientInput, approveResult) =>
                {
                    ownerDecision = approveClientInput.Decision;
                });
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
                   .MatchIf((meetingId, meetingResult) => meetingId == clientMeetingId)
                   .AfterMatch((meetingId, meetingResult) => Console.WriteLine(clientMeetingId));

                Console.WriteLine(clientMeetingId);
            }

            Console.WriteLine("User Registration Done");
        }
    }
}

using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ClientOnboarding.Workflow
{
    //from:https://tallyfy.com/workflow-examples/#onboarding
    public class ClientOnboardingWorkflow : ResumableFunction
    {
        private readonly IClientOnboardingService service;

        public ClientOnboardingWorkflow(IClientOnboardingService service)
        {
            this.service = service;
        }

        [ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            yield return WaitUserRegistration();
            OwnerTaskId = service.AskOwnerToApproveClient(RegistrationResult.FormId);

            yield return WaitOwnerApproveClient();
            if (OwnerApprovalInput.Decision is false)
            {
                service.InformUserAboutRejection(RegistrationForm.UserId);
            }
            else if (OwnerApprovalInput.Decision is true)
            {
                service.SendWelcomePackage(RegistrationForm.UserId);
                ClientMeetingId = service.SetupInitalMeetingAndAgenda(RegistrationForm.UserId);

                yield return WaitMeetingResult();
                Console.WriteLine(MeetingResult);
            }

            Console.WriteLine("User Registration Done");
        }

        private MethodWait<RegistrationForm, RegistrationResult> WaitUserRegistration()
        {
            return Wait<RegistrationForm, RegistrationResult>("Wait User Registration", service.ClientFillsForm)
                            .MatchIf((regForm, regResult) => regResult.FormId > 0)
                            .SetData((regForm, regResult) => RegistrationForm == regForm && RegistrationResult == regResult);
        }

        private MethodWait<OwnerApproveClientInput, OwnerApproveClientResult> WaitOwnerApproveClient()
        {
            return Wait<OwnerApproveClientInput, OwnerApproveClientResult>("Wait Owner Approve Client", service.OwnerApproveClient)
                            .MatchIf((approveClientInput, approveResult) => approveClientInput.TaskId == OwnerTaskId.Id)
                            .SetData((approveClientInput, approveResult) => 
                                OwnerTaskResult == approveResult && 
                                OwnerApprovalInput == approveClientInput);
        }

        private MethodWait<int, MeetingResult> WaitMeetingResult()
        {
            return Wait<int, MeetingResult>("Wait Meeting Result", service.SendMeetingResult)
                               .MatchIf((mmetingId, meetingResult) => mmetingId == ClientMeetingId.MeetingId)
                               .SetData((mmetingId, meetingResult) => MeetingResult == meetingResult);
        }

        public RegistrationForm RegistrationForm { get; set; }
        public RegistrationResult RegistrationResult { get; set; }
        public TaskId OwnerTaskId { get; set; }
        public OwnerApproveClientResult OwnerTaskResult { get; set; }
        public ClientMeetingId ClientMeetingId { get; set; }
        public MeetingResult MeetingResult { get; set; }
        public OwnerApproveClientInput OwnerApprovalInput { get; set; }
    }
}

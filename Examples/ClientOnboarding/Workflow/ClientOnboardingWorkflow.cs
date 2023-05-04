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

        public RegistrationForm RegistrationForm { get; set; }
        public RegistrationResult RegistrationResult { get; set; }
        public TaskId OwnerTaskId { get; set; }
        public OwnerApproveClientResult OwnerTaskResult { get; set; }
        public ClientMeetingId ClientMeetingId { get; set; }
        public MeetingResult MeetingResult { get; set; }


        [ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            yield return WaitUserRegistration();
            OwnerTaskId = service.AskOwnerToApproveClient(RegistrationResult.FormId);

            yield return WaitOwnerApproveClient();
            if (OwnerTaskResult.Decision is false)
            {
                service.InformUserAboutRejection(RegistrationForm.UserId);
            }
            else if (OwnerTaskResult.Decision is true)
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

        private MethodWait<int, OwnerApproveClientResult> WaitOwnerApproveClient()
        {
            return Wait<int, OwnerApproveClientResult>("Wait Owner Approve Client", service.OwnerApproveClient)
                            .MatchIf((taskId, approveResult) => taskId == OwnerTaskId.Id)
                            .SetData((taskId, approveResult) => OwnerTaskResult == approveResult);
        }

        private MethodWait<int, MeetingResult> WaitMeetingResult()
        {
            return Wait<int, MeetingResult>("Wait Meeting Result", service.SendMeetingResult)
                               .MatchIf((mmetingId, meetingResult) => mmetingId == ClientMeetingId.MeetingId)
                               .SetData((mmetingId, meetingResult) => MeetingResult == meetingResult);
        }
    }
}

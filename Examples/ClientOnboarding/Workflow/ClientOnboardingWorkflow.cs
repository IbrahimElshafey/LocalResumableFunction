using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ClientOnboarding.Workflow
{
    public class ClientOnboardingWorkflow : ResumableFunction
    {
        private readonly ClientOnboardingService service;

        public ClientOnboardingWorkflow(ClientOnboardingService service)
        {
            this.service = service;
        }

        public RegistrationForm RegitrationForm { get; set; }

        [ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
        internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
        {
            yield return
                Wait<RegistrationForm, RegistrationResult>("Wait User Registration", service.ClientFillsForm)
                .MatchIf((regForm, regResult) => regResult.FormId > -1)
                .SetData((regForm, regResult) => RegitrationForm == regForm);
            Console.WriteLine("User Registration Done");
        }
    }
}

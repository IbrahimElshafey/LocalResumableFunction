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
            registrationForm.Id = Random.Shared.Next();
            return new RegistrationResult { FormId = registrationForm.Id };
        }
    }
}

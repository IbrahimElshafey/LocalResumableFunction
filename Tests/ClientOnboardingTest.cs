using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using ResumableFunctions.TestShell;

namespace Tests
{
    public class ClientOnboardingTest
    {
        [Fact]
        public async Task TestShellWorksAsync()
        {
            var test = new TestCase(
                "Test_ClientOnboarding",
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));
            await test.Initialize();
            await test.SimulateMethodCall<ClientOnboardingService, RegistrationForm, RegistrationResult>(
                x => x.ClientFillsForm,
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });
            Assert.True(10 == 5 + 5);
        }
    }
}
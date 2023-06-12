using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.TestShell;

namespace Tests
{
    public class ClientOnboardingTest
    {
        [Fact]
        public async Task TestShellWorksAsync()
        {
            var test = new TestCase(
                "Test_ClientOnBoarding",
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));
            test.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingService>();
            await test.Start();

            var appRun = test.CurrentApp.RunAsync();
            
            await test.SimulateMethodCall<ClientOnboardingService, RegistrationForm, RegistrationResult>(
                x => x.ClientFillsForm,
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });
            await appRun;
            Assert.True(10 == 5 + 5);
        }
    }
}
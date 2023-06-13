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
            await test.ScanTypes();

            var appRun = test.CurrentApp.RunAsync();

            await test.SimulateMethodCall<ClientOnboardingService, RegistrationForm, RegistrationResult>(
                x => x.ClientFillsForm,
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });
            await test.SimulateMethodCall<ClientOnboardingService, OwnerApproveClientInput, OwnerApproveClientResult>(
                x => x.OwnerApproveClient,
                new OwnerApproveClientInput { TaskId = 5000 },
                new OwnerApproveClientResult { OwnerApprovalId = 9000 });
            await appRun;
        }
    }
}
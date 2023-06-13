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

            var callId =
                await test.SimulateMethodCall<ClientOnboardingService, RegistrationForm, RegistrationResult>(
                x => x.ClientFillsForm,
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });

            var pushedCalls = await test.GetPushedCalls();
            Assert.Single(pushedCalls);

            var instances = await test.GetInstances<ClientOnboardingWorkflow>();
            Assert.Single(instances);

            var currentInstance = instances.First();
            (currentInstance.StateObject as ClientOnboardingWorkflow).OwnerTaskId = new TaskId { Id = 6000 };
            await test.UpdateData(currentInstance);

            await test.SimulateMethodCall<ClientOnboardingService, OwnerApproveClientInput, OwnerApproveClientResult>(
                x => x.OwnerApproveClient,
                new OwnerApproveClientInput { TaskId = 6000, Decision = true },
                new OwnerApproveClientResult { OwnerApprovalId = 9000 });

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);

            var waits = await test.GetWaits(currentInstance.Id);
            Assert.Equal(3, waits.Count);
        }
    }
}
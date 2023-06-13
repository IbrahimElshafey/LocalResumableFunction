using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.TestShell;

namespace Tests
{
    public partial class ClientOnboardingTest
    {
        [Fact]
        public async Task Test_ClientOnBoarding_NoSimulate()
        {
            var test = new TestCase(
                "Test_ClientOnBoarding_NS",
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));
            test.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingService>();
            test.RegisteredServices.AddScoped<ClientOnboardingWorkflow>();
            await test.ScanTypes();

            var service = test.CurrentApp.Services.GetService<IClientOnboardingService>();
            var reg = service.ClientFillsForm(new RegistrationForm { UserId = 2000, FormData = "Form Data" });
            var currentInstance = await RoundCheck(test,1);

            var ow = service.OwnerApproveClient(new OwnerApproveClientInput { Decision = true, TaskId = currentInstance.OwnerTaskId.Id });
            currentInstance = await RoundCheck(test, 2);
        }

        private async Task<ClientOnboardingWorkflow> RoundCheck(TestCase test, int round)
        {
            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(round, pushedCalls.Count);
            var waits = await test.GetWaits();
            Assert.Equal(round + 1, waits.Count);
            var instances = await test.GetInstances<ClientOnboardingWorkflow>();
            Assert.Single(instances);
            return instances[0].StateObject as ClientOnboardingWorkflow;
        }
    }
}
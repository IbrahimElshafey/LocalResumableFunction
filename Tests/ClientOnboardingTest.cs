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
                "Test_ClientOnBoarding",
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));
            test.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingService>();
            test.RegisteredServices.AddScoped<ClientOnboardingWorkflow>();
            await test.ScanTypes();

            var service = test.CurrentApp.Services.GetService<IClientOnboardingService>();
            var reg = service.ClientFillsForm(new RegistrationForm { UserId = 2000, FormData = "Form Data" });
            var pushedCalls = await test.GetPushedCalls();
            Assert.Single(pushedCalls);
            var waits = await test.GetWaits();
            Assert.Equal(2, waits.Count);
            var instances = await test.GetInstances<ClientOnboardingWorkflow>();
            Assert.Single(instances);
            var currentInstance = instances[0].StateObject as ClientOnboardingWorkflow;

            var ow = service.OwnerApproveClient(new OwnerApproveClientInput { Decision = true, TaskId = currentInstance.OwnerTaskId.Id });
            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);
            waits = await test.GetWaits();
            Assert.Equal(3, waits.Count);
            instances = await test.GetInstances<ClientOnboardingWorkflow>();
            Assert.Single(instances);
            currentInstance = instances[0].StateObject as ClientOnboardingWorkflow;
        }
    }
}
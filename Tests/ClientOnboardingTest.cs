using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
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
            await test.ScanTypes();

            var service = test.CurrentApp.Services.GetService<IClientOnboardingService>();
            var registration = service.ClientFillsForm(new RegistrationForm { UserId = 2000, FormData = "Form Data" });
            var currentInstance = await RoundCheck(test, 1);

            var ownerApprove = service.OwnerApproveClient(new OwnerApproveClientInput { Decision = true, TaskId = currentInstance.OwnerTaskId.Id });
            currentInstance = await RoundCheck(test, 2);


            var meetingResult = service.SendMeetingResult(currentInstance.ClientMeetingId.MeetingId);
            currentInstance = await RoundCheck(test, 3, true);
        }

        private async Task<ClientOnboardingWorkflow> RoundCheck(TestCase test, int round, bool finished = false)
        {
            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(round, pushedCalls.Count);
            var waits = await test.GetWaits();
            Assert.Equal(finished ? round : round + 1, waits.Count);
            var instances = await test.GetInstances<ClientOnboardingWorkflow>();
            Assert.Single(instances);
            if (finished)
                Assert.Equal(FunctionStatus.Completed, instances[0].Status);
            return instances[0].StateObject as ClientOnboardingWorkflow;
        }
    }
}
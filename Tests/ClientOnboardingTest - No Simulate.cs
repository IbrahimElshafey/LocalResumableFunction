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
        public async Task Test_ClientOnBoarding_SimulateCalls()
        {
            var test = new TestCase(
                "Test_ClientOnBoarding",
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));
            test.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingService>();
            await test.ScanTypes();

            var callId =
                await test.SimulateMethodCall<ClientOnboardingService>(
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

            await test.SimulateMethodCall<ClientOnboardingService>(
                x => x.OwnerApproveClient,
                new OwnerApproveClientInput { TaskId = 6000, Decision = true },
                new OwnerApproveClientResult { OwnerApprovalId = 9000 });

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);

            var waits = await test.GetWaits(currentInstance.Id);
            Assert.Equal(3, waits.Count);

            var lastWait = waits.Last();
            Assert.Equal(WaitStatus.Waiting, lastWait.Status);
            Assert.Equal("Wait Meeting Result", lastWait.Name);

            (currentInstance.StateObject as ClientOnboardingWorkflow).ClientMeetingId = 
                new ClientMeetingId { MeetingId = 122, UserId = 1000 };
            await test.UpdateData(currentInstance);

            await test.SimulateMethodCall<ClientOnboardingService>(
               x => x.SendMeetingResult,
               122,
               new MeetingResult { MeetingId = 122, MeetingResultId = 155, ClientAcceptTheDeal = true, ClientRejectTheDeal = false });

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(3, pushedCalls.Count);

            currentInstance = (await test.GetInstances<ClientOnboardingWorkflow>()).Last();
            Assert.Equal(FunctionStatus.Completed, currentInstance.Status);
        }
    }
}
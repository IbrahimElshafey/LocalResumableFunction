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
        public async Task Test_ClientOnBoarding_SimulateCalls()
        {
            var test = new TestCase(
                nameof(Test_ClientOnBoarding_SimulateCalls),
                typeof(ClientOnboardingService),
                typeof(ClientOnboardingWorkflow));

            test.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingServiceFake>();

            await test.ScanTypes();

            var callId =
                await test.SimulateMethodCall<ClientOnboardingService>(
                x => x.ClientFillsForm,
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });
            var instance = await RoundCheck(test, 1);

            await test.SimulateMethodCall<ClientOnboardingService>(
                x => x.OwnerApproveClient,
                new OwnerApproveClientInput { TaskId = instance.OwnerTaskId.Id, Decision = true },
                new OwnerApproveClientResult { OwnerApprovalId = 9000 });
            instance = await RoundCheck(test, 2);

            await test.SimulateMethodCall<ClientOnboardingService>(
               x => x.SendMeetingResult,
               instance.ClientMeetingId.MeetingId,
               new MeetingResult { MeetingId = instance.ClientMeetingId.MeetingId, MeetingResultId = 155, ClientAcceptTheDeal = true, ClientRejectTheDeal = false });
            instance = await RoundCheck(test, 3, true);
        }
    }

    internal class ClientOnboardingServiceFake: ClientOnboardingService
    {
        public override TaskId AskOwnerToApproveClient(int registrationFormId)
        {
            return new TaskId { Id = 1000 };
        }
    }
}
using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Testing;

namespace Tests;

public class ClientOnboardingPrivate
{
    //Todo: Nested Closure Test
    [Fact]
    public async Task ClientOnboardingPrivate_Test()
    {
        using var testShell = new TestShell(nameof(ClientOnboardingPrivate_Test),
            typeof(ClientOnboardingWorkflowPrivate),
            typeof(IClientOnboardingService),
            typeof(ClientOnboardingService)
            );

        testShell.RegisteredServices.AddScoped<IClientOnboardingService, ClientOnboardingService>();
        await testShell.ScanTypes();
        Assert.Empty(await testShell.RoundCheck(0, 0, 0));

        var callId =
                await testShell.SimulateMethodCall<ClientOnboardingService>(
                x => x.ClientFillsForm(default),
                new RegistrationForm { FormData = "Form data", UserId = 1000 },
                new RegistrationResult { FormId = 5000 });
        Assert.Empty(await testShell.RoundCheck(1, 2, 0));

        var newWait = (await testShell.GetWaitsCreateAfterCall(callId)).FirstOrDefault();
        Assert.NotNull(newWait);
        var taskId = (newWait.ImmutableClosure as JObject)["ownerTaskId"].ToObject<int>();
        await testShell.SimulateMethodCall<ClientOnboardingService>(
            x => x.OwnerApproveClient(default),
            new OwnerApproveClientInput { TaskId = taskId, Decision = true },
            new OwnerApproveClientResult { OwnerApprovalId = 9000 });
        Assert.Empty(await testShell.RoundCheck(2, 3, 0));

        newWait = (await testShell.GetWaitsCreateAfterCall(callId)).FirstOrDefault();
        Assert.NotNull(newWait);
        var clientMeetingId = (newWait.ImmutableClosure as JObject)["clientMeetingId"].ToObject<int>();
        await testShell.SimulateMethodCall<ClientOnboardingService>(
           x => x.SendMeetingResult(default),
           clientMeetingId,
           new MeetingResult { MeetingId = clientMeetingId, MeetingResultId = 155, ClientAcceptTheDeal = true, ClientRejectTheDeal = false });
        Assert.Empty(await testShell.RoundCheck(3, 3, 1));
    }
}

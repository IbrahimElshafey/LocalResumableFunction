using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace ClientOnboarding.Workflow;
public class ClientOnboardingWorkflowPrivate : ResumableFunctionsContainer
{
    [ResumableFunctionEntryPoint("ClientOnboardingWorkflowPrivate.Start")]
    internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
    {
        int localCounter = 10;
        var userId = -1;
        yield return
            WaitMethod<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, "Wait User Registration")
            .MatchIf((_, regResult) => regResult.FormId > 0)
            .AfterMatch((regForm, regResult) =>
            {
                FormId = regResult.FormId;
                userId = regForm.UserId;
                localCounter += 10;
            });

        if (localCounter != 20)
            throw new Exception("Local var `localCounter` must be 20.");

        var ownerTaskId = _service.AskOwnerToApproveClient(FormId).Id;
        var ownerDecision = false;
        localCounter += 10;
        yield return
            WaitMethod<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, "Wait Owner Approve Client")
            .MatchIf((approveClientInput, _) => approveClientInput.TaskId == ownerTaskId)
            .AfterMatch((approveClientInput, _) =>
            {
                ownerDecision = approveClientInput.Decision;
                if (localCounter != 30)
                    throw new Exception("Local var `localCounter` must be 30.");
                localCounter += 10;
            });
        if (localCounter != 40)
            throw new Exception("Local var `localCounter` must be 40.");
        /*some code*/
        if (ownerDecision is false)
        {
            _service.InformUserAboutRejection(userId);
        }
        else if (ownerDecision)
        {
            _service.SendWelcomePackage(userId);
            var clientMeetingId = _service.SetupInitalMeetingAndAgenda(userId).MeetingId;

            yield return
                WaitMethod<int, MeetingResult>(_service.SendMeetingResult, "Wait Meeting Result")
               .AfterMatch((_, _) =>
               {
                   Console.WriteLine("Closure level 2 and public method");
                   Console.WriteLine(clientMeetingId);
                   Console.WriteLine(userId);
                   FormId += 1000;
               })
               .MatchIf((meetingId, _) => meetingId == clientMeetingId);

            Console.WriteLine(clientMeetingId);
        }
        await Task.Delay(1000);
        Console.WriteLine("User Registration Done");
    }
    private IClientOnboardingService? _service;
    public int FormId { get; set; }
    public void SetDependencies(IClientOnboardingService service)
    {
        _service = service;
    }
}

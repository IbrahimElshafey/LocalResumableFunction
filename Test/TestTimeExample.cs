using LocalResumableFunction.InOuts;

namespace Test;

internal class TestTimeExample : ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> TimeWaitTest()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        const string waitManagerOneApprovalInSeconds = "Wait manager one approval in 10 seconds";
        yield return Wait(
                waitManagerOneApprovalInSeconds,
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == input.Decision),
                Wait(TimeSpan.FromSeconds(10))
            .SetData(() => TimerMatched == true)
        ).First();

        if (TimerMatched)
        {
            WriteMessage("Timer matched");
            TimerMatched = false;
            yield return GoBackBefore(waitManagerOneApprovalInSeconds);
        }
        else
        {
            WriteMessage($"Manager one approved project with decision ({ManagerOneApproval})");
        }
        Success(nameof(TimeWaitTest));
    }

    public bool TimerMatched { get; set; }
}
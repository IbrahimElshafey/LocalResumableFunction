using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.InOuts;

namespace TestApi1.Examples;

internal class TestTimeExample : ProjectApprovalExample
{
    [ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TimeWaitTest()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        const string waitManagerOneApprovalInSeconds = "Wait manager one approval in 10 seconds";
        yield return Wait(
                waitManagerOneApprovalInSeconds,
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output),
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
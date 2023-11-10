using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class TestTimeExample : ProjectApprovalExample
{
    [ResumableFunctionEntryPoint("TestTimeExample.TimeWaitTest")]
    public async IAsyncEnumerable<Wait> TimeWaitTest()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted in TimeWaitTest")
                .MatchIf((input, output) => output == true)
                .AfterMatch((project, outputResult) => CurrentProject = project);

        ask_manager_to_approve:
        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        yield return
        Wait(
            "Wait manager one approval in 2 days",
            new[]
            {
                Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerOneApproval = output),
                Wait(TimeSpan.FromDays(2), "Two Days")
                    .AfterMatch((_, _) => TimerMatched = true)
            }
        ).MatchAny();

        if (TimerMatched)
        {
            WriteMessage("Timer matched");
            TimerMatched = false;
            goto ask_manager_to_approve;
        }
        else
        {
            WriteMessage($"Manager one approved project with decision ({ManagerOneApproval})");
        }
        Success(nameof(TimeWaitTest));
    }

    public bool TimerMatched { get; set; }
}
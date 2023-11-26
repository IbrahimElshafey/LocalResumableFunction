using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class TestLoopsExample : ProjectApprovalExample
{
    public int Counter { get; set; }
    public async IAsyncEnumerable<Wait> WaitManagerOneThreeTimeApprovals()
    {
        await Task.Delay(10);
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, "Project Submitted")
                .AfterMatch((input, output) => CurrentProject = input);

        for (; Counter < 3; Counter++)
        {
            yield return
                WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject, $"Wait Manager Approval {Counter + 1}")
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerOneApproval = output);
        }
        Success(nameof(WaitManagerOneThreeTimeApprovals));
    }
}
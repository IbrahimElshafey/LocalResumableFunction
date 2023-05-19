using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples;

internal class TestLoopsExample : ProjectApprovalExample
{
    public int Counter { get; set; }
    public async IAsyncEnumerable<Wait> WaitManagerOneThreeTimeApprovals()
    {
        await Task.Delay(10);
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .SetData((input, output) => CurrentProject == input);

        for (; Counter < 3; Counter++)
        {
            yield return
                Wait<ApprovalDecision, bool>($"Wait Manager Approval {Counter + 1}", ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output);
        }
        Success(nameof(WaitManagerOneThreeTimeApprovals));
    }
}
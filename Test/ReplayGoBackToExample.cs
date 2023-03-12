using LocalResumableFunction.InOuts;

namespace Test;

internal class ReplayGoBackToExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    public async IAsyncEnumerable<Wait> TestReplay_GoBackToGroup()
    {
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        WriteMessage("Wait first manager of three to approve");
        yield return Wait(
            "Wait first approval in three managers",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == input.Decision)
        ).First();

        var approvals = ManagerOneApproval || ManagerTwoApproval || ManagerThreeApproval;
        if (!approvals)
        {
            WriteMessage("Go back to wait three approvals again");
            yield return GoBackTo("Wait first approval in three managers");
        }
        else
        {
            WriteMessage("Project approved.");
        }
        Success(nameof(TestReplay_GoBackToGroup));
    }

    public async IAsyncEnumerable<Wait> TestReplay_GoBackTo()
    {
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .SetData((input, output) => ManagerOneApproval == input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage("Manager one rejected project and replay will go to ManagerOneApproveProject.");
            yield return GoBackTo("ManagerOneApproveProject");
        }
        else
        {
            WriteMessage("Manager one approved project");
        }
        Success(nameof(TestReplay_GoBackTo));
    }
}
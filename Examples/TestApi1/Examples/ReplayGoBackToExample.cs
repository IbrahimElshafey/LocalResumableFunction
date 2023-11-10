using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class ReplayGoBackToExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    public async IAsyncEnumerable<Wait> TestReplay_GoBackToGroup()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);

        WriteMessage("Wait first manager of three to approve");
    WaitFirstApprovalInThree:
        yield return Wait(
            new[]
            {
                Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerOneApproval = input.Decision),
                Wait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerTwoApproval = input.Decision),
                Wait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerThreeApproval = input.Decision)
            }
,
            "Wait first approval in three managers").MatchAny();

        var approvals = ManagerOneApproval || ManagerTwoApproval || ManagerThreeApproval;
        if (!approvals)
        {
            WriteMessage("Go back to wait three approvals again");
            goto WaitFirstApprovalInThree;
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
            Wait<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
    Manager_One_Approve_Project:
        yield return Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "ManagerOneApproveProject")
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .AfterMatch((input, output) => ManagerOneApproval = input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage("Manager one rejected project and replay will go to ManagerOneApproveProject.");
            goto Manager_One_Approve_Project;
        }
        else
        {
            WriteMessage("Manager one approved project");
        }
        Success(nameof(TestReplay_GoBackTo));
    }

    public async IAsyncEnumerable<Wait> TestReplay_GoBackToNewMatch()
    {
    Project_Submitted:
        yield return
            Wait<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf(CurrentProject == null, (input, output) => output == true)
                .MatchIf(CurrentProject != null, (input, output) => input.IsResubmit && input.Id == CurrentProject.Id)
                .AfterMatch((input, output) => CurrentProject = input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        yield return Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "ManagerOneApproveProject")
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .AfterMatch((input, output) => ManagerOneApproval = input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage("Manager one rejected project and replay will go to ProjectSubmitted with new match.");
            goto Project_Submitted;
        }
        else
        {
            WriteMessage("Manager one approved project");
        }
        Success(nameof(TestReplay_GoBackToNewMatch));
    }
}
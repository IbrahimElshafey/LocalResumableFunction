using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class ReplayGoBackBeforeNewMatchExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackBefore()
    {
    Project_Submitted:
        WriteMessage("Before project submitted.");
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf(CurrentProject == null, (input, output) => output == true && input.IsResubmit == false)
                .MatchIf(CurrentProject != null, (input, output) => input.Id == CurrentProject.Id && input.IsResubmit == true)
                .AfterMatch((input, output) => CurrentProject = input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        yield return WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject, "ManagerOneApproveProject")
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .AfterMatch((input, output) => ManagerOneApproval = input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage(
                "ReplayExample: Manager one rejected project and replay will wait ProjectSumbitted again.");
            goto Project_Submitted;
        }
        else
        {
            WriteMessage("ReplayExample: Manager one approved project");
        }
        Success(nameof(ReplayGoBackBeforeNewMatchExample));
    }
}
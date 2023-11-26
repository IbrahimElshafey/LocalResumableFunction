using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class ReplayGoBackAfterExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackAfter()
    {
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);

    AskManagerApprovalLabel:
        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        yield return WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject, "ManagerOneApproveProject")
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .AfterMatch((input, output) => ManagerOneApproval = input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage("Manager one rejected project and replay will go after ProjectSubmitted.");
            goto AskManagerApprovalLabel;
        }
        else
        {
            WriteMessage("Manager one approved project");
        }
        Success(nameof(TestReplay_GoBackAfter));
    }
}
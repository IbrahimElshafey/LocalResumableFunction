using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class WaitSameEventAgain : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> Test_WaitSameEventAgain()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, ProjectSumbitted)
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);

        Wait ManagerApproval() => Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "ManagerOneApproveProject")
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .AfterMatch((input, output) => ManagerOneApproval = input.Decision);
        yield return ManagerApproval();

        if (ManagerOneApproval is false)
        {
            WriteMessage("Manager one rejected project and replay will wait ManagerApproval again.");
            yield return ManagerApproval();
        }
        else
        {
            WriteMessage("Manager one approved project");
        }
        Success(nameof(Test_WaitSameEventAgain));
    }
}
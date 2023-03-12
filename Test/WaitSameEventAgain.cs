using LocalResumableFunction.InOuts;

namespace Test;

internal class WaitSameEventAgain : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> Test_WaitSameEventAgain()
    {
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);

        Wait ManagerApproval() => Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .SetData((input, output) => ManagerOneApproval == input.Decision);
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
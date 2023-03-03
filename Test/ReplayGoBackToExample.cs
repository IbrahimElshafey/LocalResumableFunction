using LocalResumableFunction.InOuts;

namespace Test;

internal class ReplayGoBackToExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackTo()
    {
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .If((input, output) => input.ProjectId == CurrentProject.Id)
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
using ResumableFunctions.Core;
using ResumableFunctions.Core.InOuts;

namespace TestApi1.Examples;

internal class ReplayGoBackBeforeNewMatchExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackBefore()
    {
        WriteMessage("Before project submitted.");
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .MatchIf((input, output) => output == true && input.IsResubmit == false)
                .SetData((input, output) => CurrentProject == input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .SetData((input, output) => ManagerOneApproval == input.Decision);

        if (ManagerOneApproval is false)
        {
            WriteMessage(
                "ReplayExample: Manager one rejected project and replay will wait ProjectSumbitted again.");
            yield return
                GoBackBefore<Project, bool>(
                    ProjectSumbitted,
                    (input, output) => input.Id == CurrentProject.Id && input.IsResubmit == true);
        }
        else
        {
            WriteMessage("ReplayExample: Manager one approved project");
        }
        Success(nameof(ReplayGoBackBeforeNewMatchExample));
    }
}
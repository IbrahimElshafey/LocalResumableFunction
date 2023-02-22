using LocalResumableFunction;
using LocalResumableFunction.InOuts;

namespace Test;

internal class ReplayGoBackBeforeNewMatchExample : ProjectApprovalExample
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackBefore()
    {
        Console.WriteLine("Before project submitted.");
        yield return
            Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .If((input, output) => output == true && input.IsResubmit == false)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .If((input, output) => input.ProjectId == CurrentProject.Id)
            .SetData((input, output) => ManagerOneApproval == input.Decision);

        if (ManagerOneApproval is false)
        {
            Console.WriteLine(
                "ReplayExample: Manager one rejected project and replay will wait ProjectSumbitted again.");
            yield return
                GoBackBefore<Project, bool>(
                    ProjectSumbitted,
                    (input, output) => input.Id == CurrentProject.Id && input.IsResubmit == true);
        }
        else
        {
            Console.WriteLine("ReplayExample: Manager one approved project");
        }
        Success(nameof(ReplayGoBackBeforeNewMatchExample));
    }
}
using LocalResumableFunction;
using LocalResumableFunction.InOuts;

namespace Test;

internal class ReplayGoBackAfterExample : Example
{
    private const string ProjectSumbitted = "Project Sumbitted";

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> TestReplay_GoBackAfter()
    {
        yield return
            When<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        yield return When<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
            .If((input, output) => output == true)
            .SetData((input, output) => ManagerOneApproval == input.Decision);

        if (ManagerOneApproval is false)
        {
            Console.WriteLine("Manager one rejected project and repaly will go after ProjectSumbitted.");
            yield return GoBackAfter(ProjectSumbitted);
        }
        else
        {
            Console.WriteLine("Manager one approved project");
        }
        Success(nameof(TestReplay_GoBackAfter));
    }
}
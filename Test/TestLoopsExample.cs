using LocalResumableFunction;
using LocalResumableFunction.InOuts;

namespace Test;

internal class TestLoopsExample:ProjectApprovalExample
{
    public int Counter { get; set; }
    public async IAsyncEnumerable<Wait> WaitManagerOneThreeTimeApprovals()
    {
        await Task.Delay(10);
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        for (; Counter < 3; Counter++)
        {
            yield return 
                Wait<ApprovalDecision,bool>($"Wait Manager Approval {Counter + 1}", ManagerOneApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output);
        }
        Success(nameof(WaitManagerOneThreeTimeApprovals));
    }
}
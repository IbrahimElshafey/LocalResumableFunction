using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace Test;

internal class ManyWaitsTypeInGroupExample:ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> ManyWaitsTypeInGroup()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait many types in same group");
        yield return
            Wait("Many waits types",
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output),
                Wait(
                    "Wait Manager Two and Four",
                    new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                        .If((input, output) => input.ProjectId == CurrentProject.Id)
                        .SetData((input, output) => ManagerTwoApproval == output),
                    new MethodWait<ApprovalDecision, bool>(ManagerFourApproveProject)
                        .If((input, output) => input.ProjectId == CurrentProject.Id)
                        .SetData((input, output) => ManagerFourApproval == output)
                ).All(),
                Wait("Sub function Wait", ManagerThreeSubFunction));
        Success(nameof(ManyWaitsTypeInGroup));
    }

  

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerThreeSubFunction()
    {
        WriteMessage("Start ManagerThreeSubFunction");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        yield return
            Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        WriteMessage("End ManagerThreeSubFunction");
    }
}
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;

namespace Test;

internal class ManyWaitsTypeInGroupExample:ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> ManyWaitsTypeInGroup()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait many types in same group");
        yield return
            Wait("Many waits types",
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output),
                Wait(
                    "Wait Manager Two and Four",
                    new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                        .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                        .SetData((input, output) => ManagerTwoApproval == output),
                    new MethodWait<ApprovalDecision, bool>(ManagerFourApproveProject)
                        .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
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
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        yield return
            Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        WriteMessage("End ManagerThreeSubFunction");
    }
}
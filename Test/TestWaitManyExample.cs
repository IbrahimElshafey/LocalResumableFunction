using ResumableFunctions.Core.InOuts;

namespace Test;

internal class TestWaitManyExample : ProjectApprovalExample
{
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait three managers to approve");
        yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).All();
        WriteMessage("Three waits matched.");
        Success(nameof(WaitThreeMethod));
    }

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitManyAndCountExpressionDefined()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait two of three managers to approve");
        yield return Wait(
            "Wait many with complex match expression",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).When(waitGroup => waitGroup.CompletedCount == 2);
        WriteMessage("Two waits of three waits matched.");
        WriteMessage("WaitManyAndCountExpressionDefined ended.");
        Success(nameof(WaitManyAndCountExpressionDefined));
    }
}
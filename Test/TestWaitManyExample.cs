using LocalResumableFunction.InOuts;

namespace Test;

internal class TestWaitManyExample : ProjectApprovalExample
{
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait three managers to approve");
        yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
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
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("Wait two of three managers to approve");
        yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == input.Decision),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == input.Decision)
        ).When(x => x.CompletedCount == 2);
        WriteMessage("Two waits of three waits matched.");
        WriteMessage("WaitManyAndCountExpressionDefined ended.");
        Success(nameof(WaitManyAndCountExpressionDefined));
    }
}
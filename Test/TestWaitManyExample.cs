using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

internal class TestWaitManyExample : ProjectApprovalExample
{
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        Console.WriteLine("Wait three managers to approve");
        CurrentProject = GetCurrentProject();
        yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).All();
        Console.WriteLine("Three waits matched.");
        Success(nameof(WaitThreeMethod));
    }

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitManyAndCountExpressionDefined()
    {
        WriteMessage("Wait two of three managers to approve");
        CurrentProject = GetCurrentProject();
        yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).WhenMatchedCount(x => x == 2);
        WriteMessage("Two waits of three waits matched.");
        WriteMessage("WaitManyAndCountExpressionDefined ended.");
        Success(nameof(WaitManyAndCountExpressionDefined));
    }
}
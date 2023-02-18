using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

internal class TestWaitManyExample : Example
{
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        Console.WriteLine("Wait three managers to approve");
        CurrentProject = GetCurrentProject();
        yield return When(
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
        ).WaitAll();
        Console.WriteLine("Three waits matched.");
        Console.WriteLine("TestWaitMany ended.");
    }

    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitManyAndMatchExpressionDefined()
    {
        Console.WriteLine("Wait two of three managers to approve");
        CurrentProject = GetCurrentProject();
        yield return When(
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
        Console.WriteLine("Two waits of three waits matched.");
        Console.WriteLine("WaitManyAndMatchExpressionDefined ended.");
    }
}
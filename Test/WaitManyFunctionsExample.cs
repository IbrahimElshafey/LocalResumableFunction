using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

internal class WaitManyFunctionsExample:Example
{
    public async IAsyncEnumerable<Wait> WaitManyFunctions()
    {
        await Task.Delay(10);
        Console.WriteLine("Start WaitManyFunctions");
        yield return
            When<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        Console.WriteLine("After project submitted.");
        yield return WaitFunctions("Wait multiple resumable functions", FunctionOne, FunctionTwo);
        Console.WriteLine("After wait two functions.");
    }

    public async IAsyncEnumerable<Wait> WaitFirstFunction()
    {
        await Task.Delay(10);
        Console.WriteLine("Start WaitManyFunctions");
        yield return
            When<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        Console.WriteLine("After project submitted.");
        yield return 
            WaitFunctions("Wait multiple resumable functions", FunctionOne, FunctionTwo)
                .WaitFirst();
        Console.WriteLine("After wait two functions.");
    }

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> FunctionOne()
    {
        await Task.Delay(10);
        Console.WriteLine("WaitTwoManagers started");
        yield return When(
            "Wait two methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output)
        ).WaitAll();
        Console.WriteLine("Two waits matched");
    }

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> FunctionTwo()
    {
        await Task.Delay(10);
        yield return
            When<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        yield return
            When<ApprovalDecision, bool>("Manager Three Approve Project Second Approval", ManagerThreeApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
    }
}
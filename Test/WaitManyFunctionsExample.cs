using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

internal class WaitManyFunctionsExample : ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> WaitManyFunctions()
    {
        await Task.Delay(10);
        Console.WriteLine("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        Console.WriteLine("After project submitted.");
        yield return Wait("Wait multiple resumable functions", FunctionOne, ManagerThreeSubFunction);
        Success(nameof(WaitManyFunctions));
    }

    public async IAsyncEnumerable<Wait> WaitSubFunctionTwoLevels()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitSubFunctionTwoLevels");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("After project submitted.");
        yield return Wait("Wait multiple resumable functions", ManagerThreeSubFunction, ManagerOneCallSubManagerTwo);
        WriteMessage("{3}After wait multiple resumable functions");
        Success(nameof(WaitSubFunctionTwoLevels));
    }


    public async IAsyncEnumerable<Wait> WaitFirstFunction()
    {
        await Task.Delay(10);
        Console.WriteLine("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        Console.WriteLine("After project submitted.");
        yield return
            Wait("Wait multiple resumable functions", FunctionOne, ManagerThreeSubFunction)
                .WaitFirst();
        Console.WriteLine("After wait two functions.");
    }

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> FunctionOne()
    {
        await Task.Delay(10);
        Console.WriteLine("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output)
        ).All();
        Console.WriteLine("Two waits matched");
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
        WriteMessage("{2}End ManagerThreeSubFunction");
    }

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerOneCallSubManagerTwo()
    {
        WriteMessage("Start ManagerOneCallSubManagerTwo");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager One Approve Project", ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output);
        yield return Wait("Wait Sub Function ManagerTwoSub", ManagerTwoSub);
        WriteMessage("{1}End ManagerOneCallSubManagerTwo");
    }

    [SubResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerTwoSub()
    {
        WriteMessage("Start ManagerTwoSub");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager Two Approve Project1", ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output);
        yield return
            Wait<ApprovalDecision, bool>("Manager Two Approve Project2", ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output);
        WriteMessage("{0}End ManagerTwoSub");
    }
}
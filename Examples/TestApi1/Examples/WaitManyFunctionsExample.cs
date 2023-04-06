using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples;

internal class WaitManyFunctionsExample : ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> WaitManyFunctions()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("After project submitted.");
        yield return Wait("Wait multiple resumable functions", WaitManagerOneAndTwoSubFunction, ManagerThreeSubFunction);
        Success(nameof(WaitManyFunctions));
    }

    public async IAsyncEnumerable<Wait> WaitSubFunctionTwoLevels()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitSubFunctionTwoLevels");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("After project submitted.");
        yield return Wait("Wait multiple resumable functions", ManagerThreeSubFunction, ManagerOneCallSubManagerTwo);
        WriteMessage("{3}After wait multiple resumable functions");
        Success(nameof(WaitSubFunctionTwoLevels));
    }


    public async IAsyncEnumerable<Wait> WaitFirstFunction()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);
        WriteMessage("After project submitted.");
        yield return
            Wait("Wait multiple resumable functions", WaitManagerOneAndTwoSubFunction, ManagerThreeSubFunction)
                .First();
        WriteMessage("After wait two functions.");
        Success(nameof(WaitFirstFunction));
    }

    [ResumableFunction]
    internal async IAsyncEnumerable<Wait> WaitManagerOneAndTwoSubFunction()
    {
        await Task.Delay(10);
        WriteMessage("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output)
        ).All();
        WriteMessage("Two waits matched");
    }

    [ResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerThreeSubFunction()
    {
        WriteMessage("Start ManagerThreeSubFunction");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output);
        WriteMessage("{2}End ManagerThreeSubFunction");
    }

    [ResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerOneCallSubManagerTwo()
    {
        WriteMessage("Start ManagerOneCallSubManagerTwo");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager One Approve Project", ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output);
        yield return Wait("Wait Sub Function ManagerTwoSub", ManagerTwoSub);
        WriteMessage("{1}End ManagerOneCallSubManagerTwo");
    }

    [ResumableFunction]
    internal async IAsyncEnumerable<Wait> ManagerTwoSub()
    {
        WriteMessage("Start ManagerTwoSub");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>("Manager Two Approve Project1", ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output);
        yield return
            Wait<ApprovalDecision, bool>("Manager Two Approve Project2", ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output);
        WriteMessage("{0}End ManagerTwoSub");
    }
}
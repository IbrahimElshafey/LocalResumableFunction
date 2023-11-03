using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class WaitManyFunctionsExample : ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> WaitManyFunctions()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return Wait("Wait multiple resumable functions", new[] { WaitManagerOneAndTwoSubFunction, ManagerThreeSubFunction });
        Success(nameof(WaitManyFunctions));
    }

    public async IAsyncEnumerable<Wait> WaitSubFunctionTwoLevels()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitSubFunctionTwoLevels");
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return Wait("Wait multiple resumable functions", new[] { ManagerThreeSubFunction, ManagerOneCallSubManagerTwo });
        WriteMessage("{3}After wait multiple resumable functions");
        Success(nameof(WaitSubFunctionTwoLevels));
    }


    public async IAsyncEnumerable<Wait> WaitFirstFunction()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitManyFunctions");
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return
            Wait("Wait multiple resumable functions", new[] { WaitManagerOneAndTwoSubFunction, ManagerThreeSubFunction })
                .MatchAny();
        WriteMessage("After wait two functions.");
        Success(nameof(WaitFirstFunction));
    }

    [SubResumableFunction("WaitManyFunctionsExample.WaitManagerOneAndTwoSubFunction")]
    internal async IAsyncEnumerable<Wait> WaitManagerOneAndTwoSubFunction()
    {
        await Task.Delay(10);
        WriteMessage("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            new Wait[]
            {
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output),
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output)
            }
        ).MatchAll();
        WriteMessage("Two waits matched");
    }

    [SubResumableFunction("WaitManyFunctionsExample.ManagerThreeSubFunction")]
    internal async IAsyncEnumerable<Wait> ManagerThreeSubFunction()
    {
        WriteMessage("Start ManagerThreeSubFunction");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>(ManagerThreeApproveProject, "Manager Three Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output);
        WriteMessage("{2}End ManagerThreeSubFunction");
    }

    [SubResumableFunction("WaitManyFunctionsExample.ManagerOneCallSubManagerTwo")]
    internal async IAsyncEnumerable<Wait> ManagerOneCallSubManagerTwo()
    {
        WriteMessage("Start ManagerOneCallSubManagerTwo");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "Manager One Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output);
        yield return Wait("Wait Sub Function ManagerTwoSub", ManagerTwoSub);
        WriteMessage("{1}End ManagerOneCallSubManagerTwo");
    }

    [SubResumableFunction("WaitManyFunctionsExample.ManagerTwoSub")]
    internal async IAsyncEnumerable<Wait> ManagerTwoSub()
    {
        WriteMessage("Start ManagerTwoSub");
        await Task.Delay(10);
        yield return
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject, "Manager Two Approve Project1")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output);
        yield return
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject, "Manager Two Approve Project2")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output);
        WriteMessage("{0}End ManagerTwoSub");
    }
}
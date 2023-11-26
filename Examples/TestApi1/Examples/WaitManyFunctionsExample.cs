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
            WaitMethod<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return WaitGroup(new[] {

            WaitFunction(WaitManagerOneAndTwoSubFunction()),
            WaitFunction(ManagerThreeSubFunction()),
        }, "Wait multiple resumable functions");
        Success(nameof(WaitManyFunctions));
    }

    public async IAsyncEnumerable<Wait> WaitSubFunctionTwoLevels()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitSubFunctionTwoLevels");
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return WaitGroup(new[] 
        {
            WaitFunction(ManagerThreeSubFunction()),
            WaitFunction(ManagerOneCallSubManagerTwo()),
        }, "Wait multiple resumable functions");
        WriteMessage("{3}After wait multiple resumable functions");
        Success(nameof(WaitSubFunctionTwoLevels));
    }


    public async IAsyncEnumerable<Wait> WaitFirstFunction()
    {
        await Task.Delay(10);
        WriteMessage("SubFunctionTest WaitManyFunctions");
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("After project submitted.");
        yield return
            WaitGroup(new[] 
            {
                WaitFunction(WaitManagerOneAndTwoSubFunction()),
                WaitFunction(ManagerThreeSubFunction()),
            }, "Wait multiple resumable functions")
                .MatchAny();
        WriteMessage("After wait two functions.");
        Success(nameof(WaitFirstFunction));
    }

    [SubResumableFunction("WaitManyFunctionsExample.WaitManagerOneAndTwoSubFunction")]
    internal async IAsyncEnumerable<Wait> WaitManagerOneAndTwoSubFunction()
    {
        await Task.Delay(10);
        WriteMessage("WaitTwoManagers started");
        yield return WaitGroup(
            new Wait[]
            {
                WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerOneApproval = output),
                WaitMethod<ApprovalDecision, bool>(ManagerTwoApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerTwoApproval = output)
            }
,
            "Wait two methods").MatchAll();
        WriteMessage("Two waits matched");
    }

    [SubResumableFunction("WaitManyFunctionsExample.ManagerThreeSubFunction")]
    internal async IAsyncEnumerable<Wait> ManagerThreeSubFunction()
    {
        WriteMessage("Start ManagerThreeSubFunction");
        await Task.Delay(10);
        yield return
            WaitMethod<ApprovalDecision, bool>(ManagerThreeApproveProject, "Manager Three Approve Project")
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
            WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject, "Manager One Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output);
        yield return WaitFunction(ManagerTwoSub("123456"), "Wait Sub Function ManagerTwoSub");
        WriteMessage("{1}End ManagerOneCallSubManagerTwo");
    }

    [SubResumableFunction("WaitManyFunctionsExample.ManagerTwoSub")]
    internal async IAsyncEnumerable<Wait> ManagerTwoSub(string functionInput)
    {
        WriteMessage("Start ManagerTwoSub");
        await Task.Delay(10);
        yield return
            WaitMethod<ApprovalDecision, bool>(ManagerTwoApproveProject, "Manager Two Approve Project1")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) =>
                {
                    ManagerTwoApproval = output;
                    if (functionInput != "123456")
                        throw new Exception("Function input must be 123456");
                    functionInput = "789";
                });
        yield return
            WaitMethod<ApprovalDecision, bool>(ManagerTwoApproveProject, "Manager Two Approve Project2")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output);
        if (functionInput != "789")
            throw new Exception("Function input must be 789");
        WriteMessage("{0}End ManagerTwoSub");
    }
}
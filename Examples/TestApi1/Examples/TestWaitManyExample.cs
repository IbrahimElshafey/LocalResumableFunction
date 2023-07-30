using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples;

public class TestWaitManyExample : ProjectApprovalExample
{
    [ResumableFunctionEntryPoint("TestWaitManyExample.WaitThreeMethodAtStart")]
    public async IAsyncEnumerable<Wait> WaitThreeMethodAtStart()
    {
        CurrentProject = new Project
        {
            Id = 1005,
            Name = "WaitThreeMethodAtStart",
        };
        yield return Wait(
            "Wait three methods at start",
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output),
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output),
            Wait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output)
        ).All();
        WriteMessage("Three waits matched.");
        Success(nameof(WaitThreeMethodAtStart));
    }

    [ResumableFunctionEntryPoint("TestWaitManyExample.WaitThreeMethod")]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted in WaitThreeMethod")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("Wait three managers to approve");
        yield return Wait(
            "Wait three methods",
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output),
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output),
            Wait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output)
        ).All();
        WriteMessage("Three waits matched.");
        Success(nameof(WaitThreeMethod));
    }

    [ResumableFunctionEntryPoint("TestWaitManyExample.WaitManyAndGroupExpressionDefined")]
    public async IAsyncEnumerable<Wait> WaitManyAndGroupExpressionDefined()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted in WaitManyAndGroupExpressionDefined")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("Wait two of three managers to approve");
        yield return Wait(
            "Wait many with complex match expression",
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output),
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output),
            Wait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output)
        ).When(waitGroup => waitGroup.CompletedCount == 2);
        WriteMessage("Two waits of three waits matched.");
        WriteMessage("WaitManyAndCountExpressionDefined ended.");
        Success(nameof(WaitManyAndGroupExpressionDefined));
    }
}
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples;

public class TestWaitManyExample : ProjectApprovalExample
{
    //[ResumableFunctionEntryPoint("TestWaitManyExample.WaitThreeMethodAtStart")]
    public async IAsyncEnumerable<Wait> WaitThreeMethodAtStart()
    {
        CurrentProject = new Project()
        {
            Id = 1005,
            Name = "WaitThreeMethodAtStart",
        };
        yield return Wait(
            "Wait three methods at start",
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
        Success(nameof(WaitThreeMethodAtStart));
    }

    //[ResumableFunctionEntryPoint("TestWaitManyExample.WaitThreeMethod")]
    public async IAsyncEnumerable<Wait> WaitThreeMethod()
    {
        yield return
            Wait<Project, bool>("Project Submitted in WaitThreeMethod", ProjectSubmitted)
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

    //[ResumableFunctionEntryPoint("TestWaitManyExample.WaitManyAndGroupExpressionDefined")]
    public async IAsyncEnumerable<Wait> WaitManyAndGroupExpressionDefined()
    {
        yield return
            Wait<Project, bool>("Project Submitted in WaitManyAndGroupExpressionDefined", ProjectSubmitted)
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
        Success(nameof(WaitManyAndGroupExpressionDefined));
    }
}
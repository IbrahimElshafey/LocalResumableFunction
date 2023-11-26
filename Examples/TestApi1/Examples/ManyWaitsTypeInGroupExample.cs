using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class ManyWaitsTypeInGroupExample : ProjectApprovalExample
{
    public async IAsyncEnumerable<Wait> ManyWaitsTypeInGroup()
    {
        yield return
            WaitMethod<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);
        WriteMessage("Wait many types in same group");
        yield return
            WaitGroup(new[]
                {
                    WaitMethod<ApprovalDecision, bool>(ManagerOneApproveProject)
                        .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                        .AfterMatch((input, output) => ManagerOneApproval = output),
                    WaitGroup(
                        new []
                        {
                            WaitMethod<ApprovalDecision, bool>(ManagerTwoApproveProject)
                                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                                .AfterMatch((input, output) => ManagerTwoApproval = output),
                            WaitMethod<ApprovalDecision, bool>(ManagerFourApproveProject)
                                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                                .AfterMatch((input, output) => ManagerFourApproval = output)
                        },
                        "Wait Manager Two and Four"),
                     WaitFunction(ManagerThreeSubFunction(), "Sub function Wait")
                }
,
                "Many waits types").MatchAll();
        Success(nameof(ManyWaitsTypeInGroup));
    }



    [SubResumableFunction("ManyWaitsTypeInGroupExample.ManagerThreeSubFunction")]
    internal async IAsyncEnumerable<Wait> ManagerThreeSubFunction()
    {
        WriteMessage("Start ManagerThreeSubFunction");
        await Task.Delay(10);
        yield return
            WaitMethod<ApprovalDecision, bool>(ManagerThreeApproveProject, "Manager Three Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output);
        yield return
            WaitMethod<ApprovalDecision, bool>(ManagerThreeApproveProject, "Manager Three Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerThreeApproval = output);
        WriteMessage("End ManagerThreeSubFunction");
    }
}
using ResumableFunctions.Core.Attributes;

namespace TestApi1.Examples
{
    internal interface IManagerFiveApproval
    {
        [WaitMethod]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
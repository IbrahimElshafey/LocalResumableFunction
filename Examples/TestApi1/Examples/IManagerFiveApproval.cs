using ResumableFunctions.Handler.Attributes;

namespace TestApi1.Examples
{
    internal interface IManagerFiveApproval
    {
        [WaitMethod("IManagerFiveApproval.ManagerFiveApproveProject")]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
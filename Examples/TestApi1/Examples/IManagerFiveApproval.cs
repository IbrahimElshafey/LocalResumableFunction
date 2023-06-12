using ResumableFunctions.Handler.Attributes;

namespace TestApi1.Examples
{
    internal interface IManagerFiveApproval
    {
        [PushCall("IManagerFiveApproval.ManagerFiveApproveProject")]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
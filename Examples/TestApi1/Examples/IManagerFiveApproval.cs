using ResumableFunctions.Handler.Attributes;

namespace TestApi1.Examples
{
    internal interface IManagerFiveApproval
    {
        [PushCall("IManagerFiveApproval.ManagerFiveApproveProject",true)]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
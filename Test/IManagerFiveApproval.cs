using ResumableFunctions.Core.Attributes;
using Test;

namespace External
{
    internal interface IManagerFiveApproval
    {
        [WaitMethod]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
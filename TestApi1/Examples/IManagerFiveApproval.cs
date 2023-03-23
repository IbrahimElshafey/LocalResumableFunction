using ResumableFunctions.Core.Attributes;
using Test;

namespace TestApi1.Examples
{
    internal interface IManagerFiveApproval
    {
        [WaitMethod]
        bool ManagerFiveApproveProject(ApprovalDecision args);
    }
}
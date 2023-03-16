using LocalResumableFunction.Attributes;
using Test;

namespace ExternalService
{
    public class ExternalServiceClass
    {
        [ExternalWaitMethod(ClassName = "External.IManagerFiveApproval")]
        public bool ManagerFiveApproveProject(ApprovalDecision args)
        {
            return false;
        }
    }
}
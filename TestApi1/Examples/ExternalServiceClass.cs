using ResumableFunctions.Core.Attributes;
using Test;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {
        [ExternalWaitMethod(ClassName = "External.IManagerFiveApproval", AssemblyName = "Test")]
        public bool ManagerFiveApproveProject(ApprovalDecision args)
        {
            return default;
        }
    }
}
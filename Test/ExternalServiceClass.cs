using ResumableFunctions.Core.Attributes;
using Test;

namespace ExternalService
{
    public class ExternalServiceClass
    {
        [ExternalWaitMethod(ClassName = "External.IManagerFiveApproval",AssemblyName ="SomeAssembly")]
        public bool ManagerFiveApproveProject(ApprovalDecision args)
        {
            return default;
        }
    }
}
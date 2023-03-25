using ResumableFunctions.Core.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {
        [ExternalWaitMethod(ClassName = "TestApi1.Examples.IManagerFiveApproval", AssemblyName = "TestApi1")]
        public bool ManagerFiveApproveProject(ApprovalDecision args)
        {
            return default;
        }
    }
}
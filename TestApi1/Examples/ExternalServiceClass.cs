using ResumableFunctions.Core.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {

        [ExternalWaitMethod(ClassName = "TestApi2.Controllers.TestController", AssemblyName = "TestApi2")]
        public int ExternalMethodTest(object o)
        {
            return default;
        }
    }
}
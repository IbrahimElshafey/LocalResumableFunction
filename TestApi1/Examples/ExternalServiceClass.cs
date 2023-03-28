using ResumableFunctions.Core.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {

        [ExternalWaitMethod(ClassFullName = "TestApi2.Controllers.TestController", AssemblyName = "TestApi2")]
        public int ExternalMethodTest(object o)
        {
            return default;
        }

        [ExternalWaitMethod(ClassFullName = "TestApi2.Controllers.TestController", AssemblyName = "TestApi2")]
        public int ExternalMethodTest2(string o)
        {
            return default;
        }

        [ExternalWaitMethod(ClassFullName = "RefernceLibrary.CodeInDllTest", AssemblyName = "RefernceLibrary")]
        public string SayHello(string userName)
        {
            return userName;
        }
    }
}
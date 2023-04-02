using ResumableFunctions.Core.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {

        [ExternalWaitMethod("TestApi2", "TestApi2.Controllers.TestController")]
        public int ExternalMethodTest(object o)
        {
            return default;
        }

        [ExternalWaitMethod("TestApi2", "TestApi2.Controllers.TestController")]
        public int ExternalMethodTest2(string o)
        {
            return default;
        }

        [ExternalWaitMethod("ReferenceLibrary", "ReferenceLibrary.CodeInDllTest")]
        public string SayHello(string userName)
        {
            return userName;
        }

        [ExternalWaitMethod("889f52f5-be6b-41db-8312-99abc8db5883")]
        public string SayGoodby(string userName)
        {
            return userName;
        }
    }
}
using ResumableFunctions.Handler.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {

        [WaitMethod("TestController.ExternalMethodTest")]
        public int ExternalMethodTest(object o)
        {
            return default;
        }

        [WaitMethod("TestController.ExternalMethodTest2")]
        public int ExternalMethodTest2(string o)
        {
            return default;
        }

        [WaitMethod("CodeInDllTest.SayHello")]
        public string SayHello(string userName)
        {
            return userName;
        }

        [WaitMethod("CodeInDllTest.SayGoodby")]
        public string SayGoodby(string userName)
        {
            return userName;
        }
    }
}
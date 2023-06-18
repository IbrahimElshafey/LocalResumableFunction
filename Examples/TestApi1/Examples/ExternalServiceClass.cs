using ResumableFunctions.Handler.Attributes;

namespace TestApi1.Examples
{
    public class ExternalServiceClass
    {

        [PushCall("TestController.ExternalMethodTest")]
        public int ExternalMethodTest(object o)
        {
            return default;
        }

        [PushCall("TestController.ExternalMethodTest2")]
        public int ExternalMethodTest2(string o)
        {
            return default;
        }

        [PushCall("CodeInDllTest.SayHello")]
        public string SayHello(string userName)
        {
            return userName;
        }

        [PushCall("CodeInDllTest.SayGoodby")]
        public string SayGoodby(string userName)
        {
            return userName;
        }
    }
}
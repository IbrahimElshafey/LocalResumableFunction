using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace ReferenceLibrary
{
    public class CodeInDllTest : ResumableFunctionsContainer
    {
        public string UserName { get; set; }

        [ResumableFunctionEntryPoint("TestFunctionInDll")]
        public async IAsyncEnumerable<Wait> TestFunctionInDll()
        {
            yield return WaitGroup(
                new[]
                {
                    WaitMethod<string, string>(SayHello, "Wait say hello")
                    .AfterMatch((userName, helloMsg) => UserName = userName),

                    WaitMethod<string, string>(Method123, "Wait Method123")
                    .AfterMatch((input, output) => UserName = output)
                }
,
                "Wait first")
                .MatchAny();

            yield return WaitMethod<string, string>
               (SayGoodby, "Wait say goodby")
               .MatchIf((userName, helloMsg) => userName == UserName)
               .AfterMatch((userName, helloMsg) => UserName = userName)
               //.NoSetData()
               ;
            //yield return Wait<string, string>
            //    ("Wait say hello duplicate", SayHello)
            //    .MatchIf((userName, helloMsg) => userName.StartsWith("M"))
            //    .SetData((userName, helloMsg) => UserName == userName)
            //    //.NoSetData()
            //    ;
            Console.WriteLine("Done");
        }

        [PushCall("CodeInDllTest.SayHello")]
        public string SayHello(string userName)
        {
            return $"Hello, {userName}.";
        }

        [PushCall("CodeInDllTest.SayGoodby")]
        public string SayGoodby(string userName)
        {
            return $"Goodby, {userName}.";
        }

        [PushCall("PublisherController.Method123", FromExternal = true)]
        public string Method123(string input) => default;
    }
}
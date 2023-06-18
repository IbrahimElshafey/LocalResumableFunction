using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ReferenceLibrary
{
    public class CodeInDllTest : ResumableFunction
    {
        public string UserName { get; set; }

        [ResumableFunctionEntryPoint("TestFunctionInDll")]
        public async IAsyncEnumerable<Wait> TestFunctionInDll()
        {
            yield return Wait<string, string>
                ("Wait say hello", SayHello)
                .MatchIf((userName, helloMsg) => userName.StartsWith("M"))
                .SetData((userName, helloMsg) => UserName == userName)
                //.NoSetData()
                ;
            yield return Wait<string, string>
               ("Wait say goodby", SayGoodby)
               .MatchIf((userName, helloMsg) => userName == UserName)
               .SetData((userName, helloMsg) => UserName == userName)
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
    }
}
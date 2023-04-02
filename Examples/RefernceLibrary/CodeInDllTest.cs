using ResumableFunctions.Core;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.InOuts;

namespace RefernceLibrary
{
    public class CodeInDllTest : ResumableFunction
    {
        public string UserName { get; set; }

        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> TestFunctionInDll()
        {
            yield return Wait<string, string>
                ("Wait say hello", SayHello)
                .MatchIf((userName, helloMsg) => userName.StartsWith("M"))
                .SetData((userName, helloMsg) => UserName == userName)
                //.NoSetData()
                ;
            yield return Wait<string, string>
                ("Wait say hello duplicate", SayHello)
                .MatchIf((userName, helloMsg) => userName.StartsWith("M"))
                .SetData((userName, helloMsg) => UserName == userName)
                //.NoSetData()
                ;
            Console.WriteLine("Done");
        }

        [WaitMethod]
        public string SayHello(string userName)
        {
            return $"Hello, {userName}.";
        }

        [WaitMethod(TrackingIdetifier = "889f52f5-be6b-41db-8312-99abc8db5883")]
        public string SayGoodby(string userName)
        {
            return $"Goodby, {userName}.";
        }
    }
}
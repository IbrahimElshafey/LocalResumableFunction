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
                .SetData((userName, helloMsg) => UserName == userName);
            Console.WriteLine("Done");
        }

        [WaitMethod]
        public string SayHello(string userName)
        {
            return $"Hello {userName}";
        }
    }
}
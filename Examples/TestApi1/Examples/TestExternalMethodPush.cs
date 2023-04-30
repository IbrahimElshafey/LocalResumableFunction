using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples
{
    public class TestExternalMethodPush : ResumableFunction
    {

        public string Result { get; set; }

        [ResumableFunctionEntryPoint("TestExternalMethodPush.ResumableFunctionThatWaitExternal")]//Point 1
        public async IAsyncEnumerable<Wait> ResumableFunctionThatWaitExternal()
        {
            yield return
             Wait<string, string>("External method `Method123`", Method123)//Point 2
                 .MatchIf((input, output) => input.StartsWith("M"))//Point 3
                 .SetData((input, output) => Result == output);//Point 4
            Console.WriteLine($"Output is :{Result}");
            Console.WriteLine("^^^Success for ResumableFunctionThatWaitExternal^^^");
        }

        [WaitMethod("PublisherController.Method123", true)]
        public string Method123(string input)
        {
            return default;
        }
    }
}

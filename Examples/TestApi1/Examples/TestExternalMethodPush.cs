using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace TestApi1.Examples
{
    public class TestExternalMethodPush : ResumableFunctionsContainer
    {

        public string Result { get; set; }

        [ResumableFunctionEntryPoint("TestExternalMethodPush.ResumableFunctionThatWaitExternal")]
        public async IAsyncEnumerable<Wait> ResumableFunctionThatWaitExternal()
        {
            yield return
             Wait<string, string>(Method123, "External method `Method123`")
                 .MatchIf((input, output) => input[0] == 'M')
                 .SetData((input, output) => Result == output);
            Console.WriteLine($"Output is :{Result}");
            Console.WriteLine("^^^Success for ResumableFunctionThatWaitExternal^^^");
        }

        [PushCall("PublisherController.Method123", CanPublishFromExternal = true)]
        public string Method123(string input)
        {
            return default;
        }
    }
}

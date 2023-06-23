using System.Reflection;
using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;

namespace Tests
{
    public partial class ManyWaitsTests
    {
        [Fact]
        public async Task WaitThreeMethodsAtStart_Test()
        {
            var test = new TestCase(nameof(WaitThreeMethodsAtStart_Test), typeof(WaitThreeMethodsAtStart));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);
           
            var wms = new WaitThreeMethodsAtStart();
            wms.Method1("1");
            wms.Method2("2");
            wms.Method3("3");

            errors = await test.GetLogs();
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
        }
    }

    public class WaitThreeMethodsAtStart : ResumableFunction
    {
        [ResumableFunctionEntryPoint("WaitThreeAtStart")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait("Wait three methods",
                Wait<string, string>("Method 1", Method1),
                Wait<string, string>("Method 2", Method2),
                Wait<string, string>("Method 3", Method3)
                );
            await Task.Delay(100);
            Console.WriteLine("Three method done");
        }

        [PushCall("Method1")]
        public string Method1(string input) => "Method1 Call";
        [PushCall("Method2")] 
        public string Method2(string input) => "Method2 Call";
        [PushCall("Method3")] 
        public string Method3(string input) => "Method3 Call";
    }

}
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
            var test = new TestCase(nameof(WaitThreeMethodsAtStart_Test), typeof(WaitManyMethods));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethods();
            wms.Method1("1");
            wms.Method2("2");
            wms.Method3("3");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(3, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
        }

        [Fact]
        public async Task WaitTwoMethodsAfterFirst_Test()
        {
            var test = new TestCase(nameof(WaitThreeMethodsAtStart_Test), typeof(WaitManyMethods));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethods();
            wms.Method4("1");
            wms.Method5("2");
            wms.Method6("3");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(3, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Completed).Count());
            Assert.Equal(2, waits.Where(x => x.IsNode).Count());
        }
    }

    public class WaitManyMethods : ResumableFunction
    {
        [ResumableFunctionEntryPoint("WaitThreeAtStart")]
        public async IAsyncEnumerable<Wait> WaitThreeAtStart()
        {
            yield return Wait("Wait three methods",
                Wait<string, string>("Method 1", Method1),
                Wait<string, string>("Method 2", Method2),
                Wait<string, string>("Method 3", Method3)
                );
            await Task.Delay(100);
            Console.WriteLine("Three method done");
        }

        [ResumableFunctionEntryPoint("TwoMethodsAfterFirst")]
        public async IAsyncEnumerable<Wait> TwoMethodsAfterFirst()
        {
            yield return Wait<string, string>("Method 4", Method4);
            yield return Wait("Wait three methods",
                Wait<string, string>("Method 5", Method5).MatchAll(),
                Wait<string, string>("Method 6", Method6).MatchAll()
            );
            await Task.Delay(100);
        }

        [PushCall("Method1")]
        public string Method1(string input) => "Method1 Call";
        [PushCall("Method2")]
        public string Method2(string input) => "Method2 Call";
        [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
        [PushCall("Method4")] public string Method4(string input) => "Method4 Call";
        [PushCall("Method5")] public string Method5(string input) => "Method5 Call";
        [PushCall("Method6")] public string Method6(string input) => "Method6 Call";
    }

}
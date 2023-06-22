using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.TestShell;

namespace Tests
{
    public partial class TimeWaitTest
    {
        [Fact]
        public async Task TestTimeWaitAtStrat_Test()
        {
            var test = new TestCase(nameof(TestTimeWaitAtStrat_Test), typeof(TimeWaitWorkflow));
            await test.ScanTypes();
            var waits = await test.GetWaits(null, true);
            await RoundTest(test, 1);

            await Task.Delay(TimeSpan.FromSeconds(3));
            await RoundTest(test, 2);

            await Task.Delay(TimeSpan.FromSeconds(3));
            await RoundTest(test, 3);
        }

        private async Task RoundTest(TestCase test, int round)
        {
            var pushedCalls = await test.GetPushedCalls();
            var waits = await test.GetWaits(null, true);
            var instances = await test.GetInstances<TimeWaitWorkflow>(true);
            Assert.True(pushedCalls.Count == round - 1);
            Assert.True(instances.Count == round);
            Assert.True(waits.Count == round);
            var errors = await test.GetErrors();
            Assert.Empty(errors);
        }
    }

    public class TimeWaitWorkflow : ResumableFunction
    {
        [ResumableFunctionEntryPoint("TestTimeWait")]
        public async IAsyncEnumerable<Wait> TestTimeWaitAtStrat()
        {
            yield return Wait(TimeSpan.FromSeconds(3));
            Console.WriteLine("Time wait at start matched.");
        }
    }
}
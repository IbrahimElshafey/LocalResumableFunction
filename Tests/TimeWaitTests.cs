using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    public class TimeWaitTests
    {
        [Fact]
        public async Task TestTimeWaitAtStart_Test()
        {
            using var test = new TestShell(nameof(TestTimeWaitAtStart_Test), typeof(TimeWaitWorkflow));
            await test.ScanTypes();
            var timeWaitId = await RoundTest(test, 1);

            await test.SimulateMethodCall<LocalRegisteredMethods>(
                x => x.TimeWait(new TimeWaitInput { TimeMatchId = timeWaitId }), true);
            timeWaitId = await RoundTest(test, 2);

            await test.SimulateMethodCall<LocalRegisteredMethods>(
                x => x.TimeWait(new TimeWaitInput { TimeMatchId = timeWaitId }), true);
            await RoundTest(test, 3);
        }

        private async Task<string> RoundTest(TestShell test, int round)
        {
            var pushedCalls = await test.GetPushedCalls();
            var waits = await test.GetWaits(null, true);
            var instances = await test.GetInstances<TimeWaitWorkflow>(true);
            Assert.Equal(round - 1, pushedCalls.Count);
            Assert.Equal(round, waits.Count);
            Assert.Equal(round, instances.Count);
            var errors = await test.GetLogs();
            Assert.Empty(errors);
            return (waits.First(x => x.IsFirst) as MethodWait)?.MandatoryPart;
        }
    }

    public class TimeWaitWorkflow : ResumableFunction
    {
        public string TimeWaitId { get; set; }

        [ResumableFunctionEntryPoint("TestTimeWait")]
        public async IAsyncEnumerable<Wait> TestTimeWaitAtStart()
        {
            yield return
                Wait(TimeSpan.FromDays(2))
                .SetData(x => TimeWaitId == x.TimeMatchId);
            Console.WriteLine("Time wait at start matched.");
        }
    }
}
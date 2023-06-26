using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;
using static Tests.SubFunctionsTest;

namespace Tests;

public class ReplayTests
{
    [Fact]
    public async Task GoAfter_Test()
    {
        var test = new TestCase(nameof(GoAfter_Test), typeof(GoAfterFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new GoAfterFunction();
        instance.Method1("Test");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(1, pushedCalls.Count);
        var instances = await test.GetInstances<GoAfterFunction>();
        Assert.Equal(1, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(1, (instances[0].StateObject as GoAfterFunction).Counter);

        var waits = await test.GetWaits();
        Assert.Equal(1, waits.Count);
        Assert.Equal(1, waits.Count(x => x.Status == WaitStatus.Completed));

    }

    public class GoAfterFunction : ResumableFunction
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("GoAfterFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>("M1", Method1);

            Counter++;
            if (Counter < 1)
                yield return GoBackAfter("M1");
            await Task.Delay(100);
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
    }
}
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

    [Fact]
    public async Task GoBefore_Test()
    {
        var test = new TestCase(nameof(GoBefore_Test), typeof(GoBeforeFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new GoBeforeFunction();
        instance.Method1("Test1");
        instance.Method2("Test1");
        instance.Method2("Test1");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(3, pushedCalls.Count);
        var instances = await test.GetInstances<GoBeforeFunction>();
        Assert.Single(instances);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(20, (instances[0].StateObject as GoBeforeFunction).Counter);
        var waits = await test.GetWaits();
        Assert.Equal(3, waits.Count);
        Assert.Equal(3, waits.Count(x => x.Status == WaitStatus.Completed));

        instance.Method1("Test2");
        instance.Method2("Test2");
        instance.Method2("Test2");

        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(6, pushedCalls.Count);
        instances = await test.GetInstances<GoBeforeFunction>();
        Assert.Equal(2, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(20, (instances[1].StateObject as GoBeforeFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

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

    public class GoBeforeFunction : ResumableFunction
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("GoBeforeFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>("M1", Method1);

            Counter += 10;
            yield return
                Wait<string, string>("M2", Method2).MatchAll();

            if (Counter < 20)
                yield return GoBackBefore("M2");
            await Task.Delay(100);
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }
}
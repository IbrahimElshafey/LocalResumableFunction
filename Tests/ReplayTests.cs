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
    [Fact]
    public async Task GoBeforeWithNewMatch_Test()
    {
        var test = new TestCase(nameof(GoBeforeWithNewMatch_Test), typeof(GoBeforeWithNewMatchFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new GoBeforeWithNewMatchFunction();
        instance.Method1("Test1");
        instance.Method2("Test1");
        instance.Method2("Back");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(3, pushedCalls.Count);
        var instances = await test.GetInstances<GoBeforeWithNewMatchFunction>();
        Assert.Single(instances);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(20, (instances[0].StateObject as GoBeforeWithNewMatchFunction).Counter);
        var waits = await test.GetWaits();
        Assert.Equal(3, waits.Count);
        Assert.Equal(3, waits.Count(x => x.Status == WaitStatus.Completed));

        instance.Method1("Test2");
        instance.Method2("Test2");
        instance.Method2("Back");
        logs = await test.GetLogs();
        Assert.Empty(logs);
        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(6, pushedCalls.Count);
        instances = await test.GetInstances<GoBeforeWithNewMatchFunction>();
        Assert.Equal(2, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(20, (instances[1].StateObject as GoBeforeWithNewMatchFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

    }

  
    [Fact]
    public async Task ReplayGoTo_Test()
    {
        var test = new TestCase(nameof(ReplayGoTo_Test), typeof(ReplayGoToFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new ReplayGoToFunction();
        instance.Method1("Test1");
        instance.Method2("Test1");
        instance.Method2("Test1");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(3, pushedCalls.Count);
        var instances = await test.GetInstances<ReplayGoToFunction>();
        Assert.Single(instances);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(16, (instances[0].StateObject as ReplayGoToFunction).Counter);
        var waits = await test.GetWaits();
        Assert.Equal(3, waits.Count);
        Assert.Equal(3, waits.Count(x => x.Status == WaitStatus.Completed));

        instance.Method1("Test2");
        instance.Method2("Test2");
        instance.Method2("Test2");
        logs = await test.GetLogs();
        Assert.Empty(logs);
        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(6, pushedCalls.Count);
        instances = await test.GetInstances<ReplayGoToFunction>();
        Assert.Equal(2, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionStatus.Completed));
        Assert.Equal(16, (instances[1].StateObject as ReplayGoToFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

    }

    public class GoBeforeWithNewMatchFunction : ResumableFunction
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("GoBeforeWithNewMatch")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>("M1", Method1);

            Counter += 10;
            yield return
                Wait<string, string>("M2", Method2).MatchAll();

            if (Counter < 20)
                yield return GoBackBefore<string, string>("M2", (input, output) => input == "Back");
            await Task.Delay(100);
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
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

    public class ReplayGoToFunction : ResumableFunction
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("ReplayGoToFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>("M1", Method1);

            Counter += 10;
            yield return
                Wait<string, string>("M2", Method2).MatchAll();

            Counter += 3;

            if (Counter < 16)
                yield return GoBackTo("M2");

            await Task.Delay(100);
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }
}
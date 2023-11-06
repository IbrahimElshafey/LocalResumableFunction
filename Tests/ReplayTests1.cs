using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests;
public partial class ReplayTests
{
    [Fact]
    public async Task GoAfter_Test()
    {
        using var test = new TestShell(nameof(GoAfter_Test), typeof(GoAfterFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new GoAfterFunction();
        instance.Method1("Test");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(1, pushedCalls.Count);
        var instances = await test.GetInstances<GoAfterFunction>();
        Assert.Equal(1, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(2, (instances[0].StateObject as GoAfterFunction).Counter);

        var waits = await test.GetWaits();
        Assert.Equal(1, waits.Count);
        Assert.Equal(1, waits.Count(x => x.Status == WaitStatus.Completed));

    }

    [Fact]
    public async Task GoBefore_Test()
    {
        using var test = new TestShell(nameof(GoBefore_Test), typeof(GoBeforeFunction));
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
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
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
        Assert.Equal(2, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(20, (instances[1].StateObject as GoBeforeFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

    }

    [Fact]
    public async Task GoBeforeWithNewMatch_Test()
    {
        using var test = new TestShell(nameof(GoBeforeWithNewMatch_Test), typeof(GoBeforeWithNewMatchFunction));
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
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
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
        Assert.Equal(2, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(20, (instances[1].StateObject as GoBeforeWithNewMatchFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

    }


    [Fact]
    public async Task ReplayGoTo_Test()
    {
        using var test = new TestShell(nameof(ReplayGoTo_Test), typeof(ReplayGoToFunction));
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
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
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
        Assert.Equal(2, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(16, (instances[1].StateObject as ReplayGoToFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));

    }

    [Fact]
    public async Task GoToWithNewMatch_Test()
    {
        using var test = new TestShell(nameof(GoToWithNewMatch_Test), typeof(GoToWithNewMatchFunction));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new GoToWithNewMatchFunction();
        instance.Method1("Test1");
        instance.Method2("Test1");
        instance.Method2("Back");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(3, pushedCalls.Count);
        var instances = await test.GetInstances<GoToWithNewMatchFunction>();
        Assert.Single(instances);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(16, (instances[0].StateObject as GoToWithNewMatchFunction).Counter);
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
        instances = await test.GetInstances<GoToWithNewMatchFunction>();
        Assert.Equal(2, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        Assert.Equal(16, (instances[1].StateObject as GoToWithNewMatchFunction).Counter);
        waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    public class GoBeforeWithNewMatchFunction : ResumableFunctionsContainer
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("GoBeforeWithNewMatch")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>(Method1, "M1");

            Counter += 10;
            yield return
                Wait<string, string>(Method2, "M2").MatchAny();

            if (Counter < 20)
                yield return GoBackBefore<string, string>("M2", (input, output) => input == "Back");
            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }
    public class GoAfterFunction : ResumableFunctionsContainer
    {
        public int Counter { get; set; }

        [ResumableFunctionEntryPoint("GoAfterFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            int localCounter = 10;
            yield return
                Wait<string, string>(Method1, "M1")
                .AfterMatch((_, _) => localCounter += 10);

            Counter++;
            if (Counter < 2)
                yield return GoBackAfter("M1");

            if (localCounter != 20)
                throw new Exception("Closure restore in replay problem.");
            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
    }

    public class GoBeforeFunction : ResumableFunctionsContainer
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("GoBeforeFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            int localCounter = 10;
            yield return
                Wait<string, string>(Method1, "M1");

            localCounter += 10;
            Counter += 10;
            yield return
                Wait<string, string>(Method2, "M2")
                .MatchAny()
                .AfterMatch((_, _) => localCounter+=10);

            if (Counter < 20)
                yield return GoBackBefore("M2");
            if (localCounter != 50)
                throw new Exception("Local variable should be 50");
            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }

    public class GoToWithNewMatchFunction : ResumableFunctionsContainer
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("ReplayGoToFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>(Method1, "M1");

            Counter += 10;
            var x = 5;
            yield return
                Wait<string, string>(Method2, "M2")
                .MatchAny()
                .AfterMatch((_, _) =>
                {
                    if (x != 5 && x != 10)
                        throw new Exception("Closure continuation problem.");
                });

            Counter += 3;
            x *= 2;

            if (Counter < 16)
                yield return GoBackTo<string, string>("M2", (input, output) => input == "Back" && x == 10);

            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }
    public class ReplayGoToFunction : ResumableFunctionsContainer
    {
        public int Counter { get; set; }
        [ResumableFunctionEntryPoint("ReplayGoToFunction")]
        public async IAsyncEnumerable<Wait> Test()
        {
            var localCounter = 10;
            yield return
                Wait<string, string>(Method1, "M1")
                .AfterMatch((_, _) => localCounter += 10);

            Counter += 10;
            yield return
                Wait<string, string>(Method2, "M2")
                .AfterMatch((_, _) => localCounter += 10)
                .MatchAny();

            Counter += 3;
            localCounter += 10;

            if (Counter < 16)
                yield return GoBackTo("M2");

            if (localCounter != 60)
                throw new Exception("Locals continuation problem.");
            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }
}
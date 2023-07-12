using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;
using static Tests.Sequence;

namespace Tests;

public class SubFunctionsTest
{
    [Fact]
    public async Task FunctionAfterFirst_Test()
    {
        using var test = new TestShell(nameof(FunctionAfterFirst_Test), typeof(FunctionAfterFirst));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new FunctionAfterFirst();
        instance.Method2("m2");
        instance.Method3("m3");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(2, pushedCalls.Count);
        var instances = await test.GetInstances<SubFunctions>(true);
        Assert.Equal(2, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        var waits = await test.GetWaits(null, true);
        Assert.Equal(4, waits.Count);
        Assert.Equal(3, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    [Fact]
    public async Task FunctionAtStart_Test()
    {
        using var test = new TestShell(nameof(FunctionAtStart_Test), typeof(SubFunctions));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new SubFunctions();
        instance.Method1("m12");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Single(pushedCalls);
        var instances = await test.GetInstances<SubFunctions>(true);
        Assert.Equal(2, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        var waits = await test.GetWaits(null, true);
        Assert.Equal(4, waits.Count);
        Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    [Fact]
    public async Task TwoFunctionsAtFirst_Test()
    {
        using var test = new TestShell(nameof(TwoFunctionsAtFirst_Test), typeof(TwoFunctionsAtFirst));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new TwoFunctionsAtFirst();
        instance.Method1("m1");
        instance.Method2("m2");

        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(2, pushedCalls.Count);
        var instances = await test.GetInstances<SubFunctions>(true);
        Assert.Equal(2, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        var waits = await test.GetWaits(null, true);
        Assert.Equal(10, waits.Count);
        Assert.Equal(5, waits.Count(x => x.Status == WaitStatus.Completed));


        instance.Method1("m3");
        instance.Method2("m4");

        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(4, pushedCalls.Count);
        instances = await test.GetInstances<SubFunctions>(true);
        Assert.Equal(3, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionStatus.Completed));
        waits = await test.GetWaits(null, true);
        Assert.Equal(15, waits.Count);
        Assert.Equal(10, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    public class TwoFunctionsAtFirst : ResumableFunction
    {
        [ResumableFunctionEntryPoint("TwoFunctionsAtFirst")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait("Wait two sub functions", SubFunction1, SubFunction2);
            await Task.Delay(100);
        }

        [SubResumableFunction("SubFunction1")]
        public async IAsyncEnumerable<Wait> SubFunction1()
        {
            yield return Wait<string, string>(Method1, "M1").MatchAll();
        }

        [SubResumableFunction("SubFunction2")]
        public async IAsyncEnumerable<Wait> SubFunction2()
        {
            yield return Wait<string, string>(Method2, "M2").MatchAll();
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
    }

    public class FunctionAfterFirst : ResumableFunction
    {
        [ResumableFunctionEntryPoint("FunctionAfterFirst")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait<string, string>(Method2, "M2");
            yield return Wait("Wait sub function2", SubFunction2);
            await Task.Delay(100);
        }
        [SubResumableFunction("SubFunction2")]
        public async IAsyncEnumerable<Wait> SubFunction2()
        {
            yield return Wait<string, string>(Method3, "M3").MatchAll();
        }
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
        [PushCall("Method3")] public string Method3(string input) => input + "M3";
    }
    public class SubFunctions : ResumableFunction
    {
        [ResumableFunctionEntryPoint("FunctionAtStart")]
        public async IAsyncEnumerable<Wait> FunctionAtStart()
        {
            yield return Wait("Wait sub function", SubFunction);
            await Task.Delay(100);
        }

        [SubResumableFunction("SubFunction")]
        public async IAsyncEnumerable<Wait> SubFunction()
        {
            yield return Wait<string, string>(Method1, "M1").MatchAll();
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";

    }
}
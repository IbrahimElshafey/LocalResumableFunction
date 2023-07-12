using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;
using static Tests.SubFunctionsLevelsTest;

namespace Tests;

public class SubFunctionsLevelsTest
{
    [Fact]
    public async Task FunctionLevels_Test()
    {
        using var test = new TestShell(nameof(FunctionLevels_Test), typeof(FunctionLevels));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new FunctionLevels();
        instance.Method1("m1");
        instance.Method2("m2");
        instance.Method3("m3");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(3, pushedCalls.Count);
        var instances = await test.GetInstances<SubFunctionsTest.SubFunctions>(true);
        Assert.Equal(2, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        var waits = await test.GetWaits();
        Assert.Equal(6, waits.Count);
        Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));


        instance.Method1("m4");
        instance.Method2("m5");
        instance.Method3("m6");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(6, pushedCalls.Count);
        instances = await test.GetInstances<SubFunctionsTest.SubFunctions>(true);
        Assert.Equal(3, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionStatus.Completed));
        waits = await test.GetWaits();
        Assert.Equal(12, waits.Count);
        Assert.Equal(12, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    public class FunctionLevels : ResumableFunction
    {
        [ResumableFunctionEntryPoint("FunctionTwoLevels")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait("Wait sub function1", SubFunction1);
            await Task.Delay(100);
        }

        [SubResumableFunction("SubFunction1")]
        public async IAsyncEnumerable<Wait> SubFunction1()
        {
            yield return Wait<string, string>(Method1, "M1").MatchAll();
            yield return Wait("Wait sub function2", SubFunction2);
        }

        [SubResumableFunction("SubFunction2")]
        public async IAsyncEnumerable<Wait> SubFunction2()
        {
            yield return Wait<string, string>(Method2, "M2").MatchAll();
            yield return Wait("Wait sub function3", SubFunction3);
        }

        [SubResumableFunction("SubFunction3")]
        public async IAsyncEnumerable<Wait> SubFunction3()
        {
            yield return Wait<string, string>(Method3, "M2").MatchAll();
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
        [PushCall("Method3")] public string Method3(string input) => input + "M3";
    }
}
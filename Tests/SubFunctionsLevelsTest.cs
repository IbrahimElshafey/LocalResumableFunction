using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;
using ResumableFunctions.Handler.BaseUse;

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
        instance.Method1("m1_1");
        instance.Method4("m4_1");
        instance.Method2("m2_1");
        instance.Method3("m3_1");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(4, pushedCalls.Count);
        var instances = await test.GetInstances<SubFunctionsTest.SubFunctions>(true);
        Assert.Equal(2, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        var waits = await test.GetWaits();
        Assert.Equal(7, waits.Count);
        Assert.Equal(7, waits.Count(x => x.Status == WaitStatus.Completed));


        instance.Method1("m1_2");
        instance.Method4("m4_2");
        instance.Method2("m2_2");
        instance.Method3("m3_2");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        pushedCalls = await test.GetPushedCalls();
        Assert.Equal(8, pushedCalls.Count);
        instances = await test.GetInstances<SubFunctionsTest.SubFunctions>(true);
        Assert.Equal(3, instances.Count);
        Assert.Equal(2, instances.Count(x => x.Status == FunctionInstanceStatus.Completed));
        waits = await test.GetWaits();
        Assert.Equal(14, waits.Count);
        Assert.Equal(14, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    public class FunctionLevels : ResumableFunctionsContainer
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
            int x = 10;
            yield return Wait<string, string>(Method1, "M1")
                .MatchAny()
                .AfterMatch((_, _) =>
                {
                    if (x != 10)
                        throw new Exception("Closure in sub function problem.");
                });

            x += 10;
            yield return Wait<string, string>(Method4, "M4")
                .MatchAny()
                .AfterMatch((_, _) =>
                {
                    if (x != 20)
                        throw new Exception("Closure restore in sub function problem.");
                });
            yield return Wait("Wait sub function2", SubFunction2);
        }

        [SubResumableFunction("SubFunction2")]
        public async IAsyncEnumerable<Wait> SubFunction2()
        {
            yield return Wait<string, string>(Method2, "M2").MatchAny();
            yield return Wait("Wait sub function3", SubFunction3);
        }

        [SubResumableFunction("SubFunction3")]
        public async IAsyncEnumerable<Wait> SubFunction3()
        {
            yield return Wait<string, string>(Method3, "M2").MatchAny();
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
        [PushCall("Method3")] public string Method3(string input) => input + "M3";
        [PushCall("Method4")] public string Method4(string input) => input + "M4";
    }
}
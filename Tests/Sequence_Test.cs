using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests;

public class Sequence
{
    [Fact]
    public async Task SequenceFunction_Test()
    {
        using var test = new TestShell(nameof(SequenceFunction_Test), typeof(SequenceFunction));
        await test.ScanTypes();
        Assert.Empty(await test.RoundCheck(0, 0, 0));

        var function = new SequenceFunction();
        function.Method1("in1");
        Assert.Empty(await test.RoundCheck(1, 2, 0));

        function.Method2("in2");
        Assert.Empty(await test.RoundCheck(2, 3, 0));

        function.Method3("in3");
        Assert.Empty(await test.RoundCheck(3, 3, 1));
    }

    private async Task RoundTest(int round, TestShell test, bool isFinal = false)
    {
        var errorLogs = await test.GetLogs();
        Assert.Empty(errorLogs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(round, pushedCalls.Count);
        var instances = await test.GetInstances<SequenceFunction>();
        Assert.Equal(round == 0 ? 0 : 1, instances.Count);
        var waits = await test.GetWaits();
        if (isFinal == false)
            Assert.Equal(round == 0 ? 0 : round + 1, waits.Count);
        else
            Assert.Equal(3, waits.Where(x => x.Status == WaitStatus.Completed).Count());
    }

    public class SequenceFunction : ResumableFunction
    {
        [ResumableFunctionEntryPoint("ThreeMethodsSequence")]
        public async IAsyncEnumerable<Wait> ThreeMethodsSequence()
        {
            yield return Wait<string, string>("M1", Method1);
            yield return Wait<string, string>("M2", Method2).MatchAll();
            yield return Wait<string, string>("M3", Method3).MatchAll();
            await Task.Delay(100);
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
        [PushCall("Method3")] public string Method3(string input) => input + "M3";
    }
}
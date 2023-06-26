using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;
using static Tests.Sequence;

namespace Tests;

public class SubFunctionsTest
{
    [Fact]
    public async Task FunctionAtStart_Test()
    {
        var test = new TestCase(nameof(FunctionAtStart_Test), typeof(SubFunctions));
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
            yield return Wait<string, string>("M1", Method1).MatchAll();
        }

        [PushCall("Method1")] public string Method1(string input) => input + "M1";
    }
}
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;
using System.Diagnostics.Metrics;

namespace Tests;
public class ReplayInSubFunction
{
    [Fact]
    public async Task ReplayInSubFunction_Test()
    {
        using var test = new TestShell(nameof(ReplayInSubFunction_Test), typeof(TestClass));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new TestClass();
        instance.Method6("Test");
        instance.Method1("Test");
        instance.Method2("Test");
        instance.Method2("Back");
        instance.Method3("Test");
        instance.Method4("Test");
        instance.Method4("Back");
        instance.Method5("Test");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(8, pushedCalls.Count);
        var instances = await test.GetInstances<TestClass>();
        Assert.Equal(1, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
        //Assert.Equal(1, (instances[0].StateObject as ReplayInSubFunction).Counter1);

        //var waits = await test.GetWaits();
        //Assert.Equal(1, waits.Count);
        //Assert.Equal(1, waits.Count(x => x.Status == WaitStatus.Completed));
    }

    public class TestClass : ResumableFunctionsContainer
    {
        [ResumableFunctionEntryPoint("ReplayInSubFunctions")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait<string, string>(Method6, "M6");
            yield return Wait("Wait Two Paths", PathOneFunction, PathTwoFunction);//wait two sub functions
            yield return Wait<string, string>(Method5, "M5").MatchAll();
        }
        public int Counter1 { get; set; }
        public int Counter2 { get; set; }

        [SubResumableFunction("PathOneFunction")]
        public async IAsyncEnumerable<Wait> PathOneFunction()
        {
            yield return
                   Wait<string, string>(Method1, "M1").MatchAll();

            Counter1 += 10;
            yield return
                Wait<string, string>(Method2, "M2").MatchAll();

            Counter1 += 3;

            if (Counter1 < 16)
                yield return GoBackTo<string, string>("M2", (input, output) => input == "Back");

            await Task.Delay(100);
        }

        [SubResumableFunction("PathTwoFunction")]
        public async IAsyncEnumerable<Wait> PathTwoFunction()
        {
            yield return
                  Wait<string, string>(Method3, "M3").MatchAll();

            Counter2 += 10;
            yield return
                Wait<string, string>(Method4, "M4").MatchAll();

            Counter2 += 3;

            if (Counter2 < 16)
                yield return GoBackTo<string, string>("M4", (input, output) => input == "Back");

            await Task.Delay(100);
        }

        [PushCall("RequestAdded")] public string Method1(string input) => input + "M1";
        [PushCall("Method2")] public string Method2(string input) => input + "M2";
        [PushCall("Method3")] public string Method3(string input) => input + "M3";
        [PushCall("Method4")] public string Method4(string input) => input + "M4";
        [PushCall("Method5")] public string Method5(string input) => input + "M5";
        [PushCall("Method6")] public string Method6(string input) => input + "M6";
    }
}

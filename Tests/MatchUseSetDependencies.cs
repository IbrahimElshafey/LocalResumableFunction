using Newtonsoft.Json;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests;

public class MatchUseSetDependencies
{
    [Fact]
    public async Task MatchUseSetDependencies_Test()
    {
        using var test = new TestShell(nameof(MatchUseSetDependencies_Test), typeof(TestClass));
        await test.ScanTypes();

        var logs = await test.GetLogs();
        Assert.Empty(logs);

        var instance = new TestClass();
        instance.Method6("Test");

        logs = await test.GetLogs();
        Assert.Empty(logs);
        var pushedCalls = await test.GetPushedCalls();
        Assert.Equal(1, pushedCalls.Count);
        var instances = await test.GetInstances<TestClass>();
        Assert.Equal(1, instances.Count);
        Assert.Equal(1, instances.Count(x => x.Status == FunctionStatus.Completed));
    }

    public class TestClass : ResumableFunction
    {
        [MessagePack.IgnoreMember]
        public Dep1 dep1;//must be public if used in the expression trees and [MessagePack.IgnoreMember] to not serialize it
        private void SetDependencies()
        {
            dep1 = new Dep1(5);
        }

        [ResumableFunctionEntryPoint("MatchWithInstanceMethodCall")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return
                Wait<string, string>("M6", Method6)
                .MatchIf((input, output) => input == "Test" && InstanceCall(input, output) && dep1.MethodIndep(input) > 0);
        }

        private bool InstanceCall(string input, string output)
        {
            return output == "TestM6" && input.Length == 4;
        }

        [PushCall("Method6")] public string Method6(string input) => input + "M6";
    }

    public class Dep1
    {
        public Dep1(int b)
        {

        }
        public int MethodIndep(string input) => input.Length;
    }
}

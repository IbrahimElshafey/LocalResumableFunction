using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;

namespace Tests;

public class Attributes_Test
{
    [Fact]
    public async Task NotSyncFunction_Test()
    {
        var test = new TestCase(nameof(NotSyncFunction_Test), typeof(AttributesUsageClass));
        await test.ScanTypes();
        var errorLogs = await test.GetLogs();
        Assert.NotEmpty(errorLogs);
    }

    public class AttributesUsageClass:ResumableFunction
    {
        [ResumableFunctionEntryPoint("NotAsync")]
        public IAsyncEnumerable<Wait> NotAsync()
        {
            throw new NotImplementedException();
        }

        [PushCall("MethodToWait")]
        public string MethodToWait(string input)
        {
            return input?.Length.ToString();
        }
    }
}
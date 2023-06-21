using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.TestShell;

namespace Tests
{
    public partial class ExcptionInResumableFunction_Test
    {
        [Fact]
        public async Task ExceptionAtStart_Test()
        {
            var test = new TestCase(nameof(ExceptionAtStart_Test), typeof(ExcptionInResumableFunction));
            await test.ScanTypes();
            var errorLogs = await test.GetErrors();
            Assert.True(errorLogs.Count > 0);
        }
    }

    public class ExcptionInResumableFunction : ResumableFunction
    {
        [ResumableFunctionEntryPoint("ExceptionAtStart")]
        public IAsyncEnumerable<Wait> ExceptionAtStart()
        {
            throw new Exception("Can't get any wait");
        }
    }
}
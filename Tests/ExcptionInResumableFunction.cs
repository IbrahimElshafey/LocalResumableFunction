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
            var test = new TestCase(nameof(ExceptionAtStart_Test), typeof(ExcptionBeforeFirstWait));
            await test.ScanTypes();
            var errorLogs = await test.GetErrors();
            Assert.True(errorLogs.Count > 0);
        }

        [Fact]
        public async Task ExceptionAfterFirstWait_Test()
        {
            var test = new TestCase(nameof(ExceptionAfterFirstWait_Test), typeof(ExcptionAfterFirstWait));
            await test.ScanTypes();
            var errorLogs = await test.GetErrors();
            Assert.False(errorLogs.Count > 0);
            try
            {
                await test.SimulateMethodCall<ExcptionAfterFirstWait>(x => x.MethodToWait("Ibrahim"), "3");
            }
            catch (Exception)
            {
                errorLogs = await test.GetErrors();
                Assert.True(errorLogs.Count > 0);
            }
            
        }
    }

    public class ExcptionAfterFirstWait : ResumableFunction
    {
        public string? MehtodOutput { get; set; }

        [ResumableFunctionEntryPoint("ExceptionAfterFirstWait")]
        public async IAsyncEnumerable<Wait> ExceptionAfterFirstWait()
        {
            yield return Wait<string, string>("Wait Method", MethodToWait)
                .MatchIf((input, output) => input.Length > 3)
                .SetData((input, output) => MehtodOutput == output);
            await Task.Delay(100);
            throw new Exception("Can't get any wait");
        }

        [PushCall("MethodToWait")]
        public string MethodToWait(string input)
        {
            return input?.Length.ToString();
        }
    }
    public class ExcptionBeforeFirstWait : ResumableFunction
    {
        [ResumableFunctionEntryPoint("ExceptionAtStart")]
        public IAsyncEnumerable<Wait> ExceptionAtStart()
        {
            throw new Exception("Can't get any wait");
        }
    }
}
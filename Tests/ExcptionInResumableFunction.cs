using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;

namespace Tests
{
    public class ExcptionInResumableFunction_Test
    {
        [Fact]
        public async Task ExceptionAtStart_Test()
        {
            var test = new TestCase(nameof(ExceptionAtStart_Test), typeof(ExceptionBeforeFirstWait));
            await test.ScanTypes();
            var errorLogs = await test.GetLogs();
            Assert.True(errorLogs.Count > 0);
        }

        [Fact]
        public async Task ExceptionAfterFirstWait_Test()
        {
            var test = new TestCase(nameof(ExceptionAfterFirstWait_Test), typeof(ExceptionAfterFirstWait));
            await test.ScanTypes();
            var errorLogs = await test.GetLogs();
            Assert.Empty(errorLogs);
            await test.SimulateMethodCall<ExceptionAfterFirstWait>(x => x.MethodToWait("Ibrahim"), "3");
            errorLogs = await test.GetLogs();
            Assert.NotEmpty(errorLogs);
        }
    }

    public class ExceptionAfterFirstWait : ResumableFunction
    {
        public string? MethodOutput { get; set; }

        [ResumableFunctionEntryPoint("Test")]
        public async IAsyncEnumerable<Wait> Test()
        {
            yield return Wait<string, string>("Wait Method", MethodToWait)
                .MatchIf((input, output) => input.Length > 3)
                .SetData((input, output) => MethodOutput == output);
            await Task.Delay(100);
            throw new Exception("Can't get any wait");
        }

        [PushCall("MethodToWait")]
        public string MethodToWait(string input)
        {
            return input?.Length.ToString();
        }
    }
    public class ExceptionBeforeFirstWait : ResumableFunction
    {
        [ResumableFunctionEntryPoint("ExceptionAtStart")]
        public async IAsyncEnumerable<Wait> ExceptionAtStart()
        {
            throw new Exception("Can't get any wait");
            yield return Wait<string, string>("Wait Method", MethodToWait)
                .MatchIf((input, output) => input.Length > 3);
            await Task.Delay(100);
        }

        [PushCall("MethodToWait")]
        public string MethodToWait(string input)
        {
            return input?.Length.ToString();
        }
    }
}
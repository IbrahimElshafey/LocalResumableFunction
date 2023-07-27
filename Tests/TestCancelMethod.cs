using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    public class TestCancelMethod
    {
        [Fact]
        public async Task TestCancelMethod_Test()
        {
            using var test = new TestShell(nameof(TestCancelMethod_Test), typeof(Test));
            await test.ScanTypes();

            Assert.Empty(await test.RoundCheck(0, 0, 0));

            var instance = new Test();
            instance.Method1("ss");
            Assert.Empty(await test.RoundCheck(1, 4, 1));

            instance = (await test.GetInstances<Test>()).FirstOrDefault()?.StateObject as Test;
            Assert.Equal(2, instance?.Counter);
        }
        public class Test : ResumableFunctionsContainer
        {
            public int Counter { get; set; }
            [ResumableFunctionEntryPoint("WaitThreeAtStart")]
            public async IAsyncEnumerable<Wait> WaitThreeAtStart()
            {
                yield return Wait("Wait three methods",
                    Wait<string, string>(Method1, "Method 1").WhenCancel((x, y) => Counter++),
                    Wait<string, string>(Method2, "Method 2")
                    .WhenCancel((x, y) =>
                    {
                        Console.WriteLine("Method Two Cancel");
                        Counter++;
                    }),
                    Wait<string, string>(Method3, "Method 3").WhenCancel(IncrementCounter)
                    )
                .First();
                await Task.Delay(100);
                Console.WriteLine("Three method done");
            }

            private void IncrementCounter(string arg1, string arg2)
            {
                Counter++;
            }

            [PushCall("Method1")] public string Method1(string input) => "Method1 Call";
            [PushCall("Method2")] public string Method2(string input) => "Method2 Call";
            [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
        }
    }

}
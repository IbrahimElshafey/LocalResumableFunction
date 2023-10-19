using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
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
            instance.Method4("44");
            Assert.Empty(await test.RoundCheck(2, 5, 1));

            instance = (await test.GetInstances<Test>()).FirstOrDefault()?.StateObject as Test;
            Assert.Equal(2, instance?.Counter);
        }

        public class Test : ResumableFunctionsContainer
        {
            public int Counter { get; set; }
            [ResumableFunctionEntryPoint("TestCancelMethod")]
            public async IAsyncEnumerable<Wait> TestCancelMethod()
            {
                var dateTime = DateTime.Now;
                int x = 2;
                yield return Wait("Wait three methods",
                    Wait<string, string>(Method1, "Method 1")
                        .MatchIf((_, _) => dateTime < new DateTime(2025, 1, 1))
                        .WhenCancel(() => Counter += x - 1)//counter=1
                        .AfterMatch(StaticAfterMatch),
                    Wait<string, string>(Method2, "Method 2")
                        .MatchAny()
                        .WhenCancel(() =>
                        {
                            Console.WriteLine("Method Two Cancel");
                            Counter += x;//counter=2
                        })
                        ,
                    Wait<string, string>(Method3, "Method 3")
                        .WhenCancel(StaticIncrementCounter)
                    )
                .MatchAny();

                var ran = new Random(10).Next(10, 50);
                yield return
                    Wait<string, string>(Method4, "Method 4")
                    .MatchIf((input, output) => input.Length > 1 && ran >= 10)
                    .AfterMatch((_, _) =>
                    {
                        Console.WriteLine("After match");
                        if (x != 2)
                            throw new Exception("Closure continuation not restored.");
                    });
                await Task.Delay(100);
                Console.WriteLine("Three method done");
            }

            private static void StaticAfterMatch(string arg1, string arg2)
            {
                Console.WriteLine($"{arg1}:{arg2}");
            }

            private static void StaticIncrementCounter()
            {
                Console.WriteLine("Static call");
            }
            private void IncrementCounter()
            {
                Counter++;
            }

            [PushCall("Method1")] public string Method1(string input) => "Method1 Call";
            [PushCall("Method2")] public string Method2(string input) => "Method2 Call";
            [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
            [PushCall("Method4")] public string Method4(string input) => "Method4 Call";
        }
    }

}
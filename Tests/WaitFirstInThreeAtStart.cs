using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    //todo: update this test
    public class WaitFirstInThreeAtStart
    {
        [Fact]
        public async Task WaitFirstInThreeAtStart_Test()
        {
            using var testShell = new TestShell(nameof(WaitFirstInThreeAtStart_Test), typeof(Test));
            await testShell.ScanTypes();
            var errors = await testShell.GetLogs();
            Assert.Empty(errors);

            var testInstance = new Test();
            testInstance.Method7("1");

            var pushedCalls = await testShell.GetPushedCalls();
            Assert.Single(pushedCalls);
            errors = await testShell.GetLogs();
            Assert.Empty(errors);
            var waits = await testShell.GetWaits();
            Assert.Equal(4, waits.Count);
            Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Completed));
            Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Canceled));
            Assert.Equal(1, waits.Count(x => x.IsRootNode));
            var instance = await testShell.GetFirstInstance<Test>();
            Assert.Equal(1, instance.Counter);
            Assert.Equal(2, instance.CancelCounter);

            //round two
            testInstance.Method8("1");
            pushedCalls = await testShell.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);
            errors = await testShell.GetLogs();
            Assert.Empty(errors);
            waits = await testShell.GetWaits();
            Assert.Equal(8, waits.Count);
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Completed).Count());
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Canceled).Count());
            Assert.Equal(2, waits.Where(x => x.IsRootNode).Count());
        }
        public class Test : ResumableFunctionsContainer
        {

            public int Counter { get; set; }
            public int CancelCounter { get; set; }
            [ResumableFunctionEntryPoint("WaitFirstInThree")]
            public async IAsyncEnumerable<Wait> WaitFirstInThree()
            {
                int localInCancel = 10;
                int localInMatch = 10;
                yield return Wait("Wait First In Three",
                    Wait<string, string>(Method7, "Method 7")
                    .AfterMatch((_, _) => { Counter++; localInMatch++; })
                    .WhenCancel(() => { CancelCounter++; localInCancel++; }),
                    Wait<string, string>(Method8, "Method 8")
                    .AfterMatch((_, _) => { Counter++; localInMatch++; })
                    .WhenCancel(() => { CancelCounter++; localInCancel++; }),
                    Wait<string, string>(Method9, "Method 9")
                    .AfterMatch((_, _) => { Counter++; localInMatch++; })
                    .WhenCancel(() => { CancelCounter++; localInCancel++; })
                ).MatchAny();

                //if (localInMatch != 11)
                //    throw new Exception("Local variable not saved in match in wait first group.");
                //if (localInCancel != 12)
                //    throw new Exception("Local variable not saved in cancel in wait first group.");
                await Task.Delay(100);
                Console.WriteLine("WaitFirstInThree");
            }

            [PushCall("Method7")] public string Method7(string input) => "Method7 Call";
            [PushCall("Method8")] public string Method8(string input) => "Method8 Call";
            [PushCall("Method9")] public string Method9(string input) => "Method9 Call";
        }

    }

}
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    //todo: update this test
    public class WaitManyMethodsWithExpression
    {
        [Fact]
        public async Task WaitManyMethodsWithExpression_Test()
        {
            using var test = new TestShell(nameof(WaitManyMethodsWithExpression_Test), typeof(Test));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new Test();
            wms.Method2("1");
            wms.Method3("1");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
            Assert.Equal(3, waits.Count(x => x.Status == WaitStatus.Completed));
            Assert.Equal(1, waits.Count(x => x.Status == WaitStatus.Canceled));
            Assert.Equal(1, waits.Count(x => x.IsRoot));


            wms.Method3("1");
            wms.Method1("1");

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(4, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            waits = await test.GetWaits();
            Assert.Equal(8, waits.Count);
            Assert.Equal(6, waits.Count(x => x.Status == WaitStatus.Completed));
            Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Canceled));
            Assert.Equal(2, waits.Count(x => x.IsRoot));
        }
        public class Test : ResumableFunctionsContainer
        {
            public int Id { get; set; } = 10;
            [ResumableFunctionEntryPoint("WaitManyWithExpression")]
            public async IAsyncEnumerable<Wait> WaitManyWithExpression()
            {
                int x = 1;
                yield return Wait("Wait three methods",
                    Wait<string, string>(Method1, "Method 1"),
                    Wait<string, string>(Method2, "Method 2"),
                    Wait<string, string>(Method3, "Method 3")
                )
                //.MatchIf(group => group.CompletedCount == 2 && Id == 10 && x == 1);
                .MatchIf(group =>
                {
                    if (x != 1)
                        throw new Exception("Closure in group match filter not work");
                    return group.CompletedCount == 2 && Id == 10;
                });
                //.MatchIf(group => group.CompletedCount == 2);
                await Task.Delay(100);
                Console.WriteLine(x);
                Console.WriteLine("Three method done");
            }

            [PushCall("RequestAdded")]
            public string Method1(string input) => "RequestAdded Call";
            [PushCall("Method2")]
            public string Method2(string input) => "Method2 Call";
            [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
        }

    }

}
using System.Reflection;
using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    public class ManyWaitsTests
    {
        [Fact]
        public async Task WaitThreeMethodsAtStart_Test()
        {
            using var test = new TestShell(nameof(WaitThreeMethodsAtStart_Test), typeof(WaitManyMethods));
            await test.ScanTypes("WaitThreeAtStart");
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethods();
            wms.Method1("1");
            wms.Method2("2");
            wms.Method3("3");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(3, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);

            wms.Method1("1");
            wms.Method2("2");
            wms.Method3("3");

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(6, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            waits = await test.GetWaits();
            Assert.Equal(8, waits.Count);
        }

        [Fact]
        public async Task WaitTwoMethodsAfterFirst_Test()
        {
            using var test = new TestShell(nameof(WaitTwoMethodsAfterFirst_Test), typeof(WaitManyMethods));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethods();
            wms.Method4("1");
            wms.Method5("2");
            wms.Method6("3");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(3, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Completed).Count());
            Assert.Equal(2, waits.Where(x => x.IsRootNode).Count());

            wms = new WaitManyMethods();
            wms.Method4("1");
            wms.Method5("2");
            wms.Method6("3");

            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(6, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            waits = await test.GetWaits();
            Assert.Equal(8, waits.Count);
            Assert.Equal(8, waits.Where(x => x.Status == WaitStatus.Completed).Count());
            Assert.Equal(4, waits.Where(x => x.IsRootNode).Count());
        }

        [Fact]
        public async Task WaitFirstInThreeAtStart_Test()
        {
            using var test = new TestShell(nameof(WaitFirstInThreeAtStart_Test), typeof(WaitManyMethods));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethods();
            wms.Method7("1");

            var pushedCalls = await test.GetPushedCalls();
            Assert.Equal(1, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            var waits = await test.GetWaits();
            Assert.Equal(4, waits.Count);
            Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Completed));
            Assert.Equal(2, waits.Count(x => x.Status == WaitStatus.Canceled));
            Assert.Equal(1, waits.Count(x => x.IsRootNode));

            //round two
            wms.Method8("1");
            pushedCalls = await test.GetPushedCalls();
            Assert.Equal(2, pushedCalls.Count);
            errors = await test.GetLogs();
            Assert.Empty(errors);
            waits = await test.GetWaits();
            Assert.Equal(8, waits.Count);
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Completed).Count());
            Assert.Equal(4, waits.Where(x => x.Status == WaitStatus.Canceled).Count());
            Assert.Equal(2, waits.Where(x => x.IsRootNode).Count());
        }

        [Fact]
        public async Task WaitManyMethodsWithExpression_Test()
        {
            using var test = new TestShell(nameof(WaitManyMethodsWithExpression_Test), typeof(WaitManyMethodsWithExpression));
            await test.ScanTypes();
            var errors = await test.GetLogs();
            Assert.Empty(errors);

            var wms = new WaitManyMethodsWithExpression();
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
            Assert.Equal(1, waits.Count(x => x.IsRootNode));


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
            Assert.Equal(2, waits.Count(x => x.IsRootNode));
        }
    }

    public class WaitManyMethodsWithExpression : ResumableFunctionsContainer
    {
        public int Id { get; set; } = 10;
        [ResumableFunctionEntryPoint("WaitManyWithExpression")]
        public async IAsyncEnumerable<Wait> WaitThreeAtStart()
        {
            int x = 1;
            yield return Wait("Wait three methods",
                Wait<string, string>(Method1, "Method 1"),
                Wait<string, string>(Method2, "Method 2"),
                Wait<string, string>(Method3, "Method 3")
            )
            //.MatchIf(group => group.CompletedCount == 2 && Id == 10 && x == 1);
            .MatchIf(group => group.CompletedCount == 2 && Id == 10);
            //.MatchIf(group => group.CompletedCount == 2);
            await Task.Delay(100);
            Console.WriteLine("Three method done");
        }

        [PushCall("RequestAdded")]
        public string Method1(string input) => "RequestAdded Call";
        [PushCall("Method2")]
        public string Method2(string input) => "Method2 Call";
        [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
    }
    public class WaitManyMethods : ResumableFunctionsContainer
    {
        [ResumableFunctionEntryPoint("WaitThreeAtStart")]
        public async IAsyncEnumerable<Wait> WaitThreeAtStart()
        {
            yield return Wait("Wait three methods",
                Wait<string, string>(Method1, "Method 1"),
                Wait<string, string>(Method2, "Method 2"),
                Wait<string, string>(Method3, "Method 3")
                );
            await Task.Delay(100);
            Console.WriteLine("Three method done");
        }

        [ResumableFunctionEntryPoint("TwoMethodsAfterFirst")]
        public async IAsyncEnumerable<Wait> TwoMethodsAfterFirst()
        {
            yield return Wait<string, string>(Method4, "Method 4");
            yield return Wait("Two Methods After First",
                Wait<string, string>(Method5, "Method 5").MatchAll(),
                Wait<string, string>(Method6, "Method 6").MatchAll()
            );
            await Task.Delay(100);
        }

        [ResumableFunctionEntryPoint("WaitFirstInThree")]
        public async IAsyncEnumerable<Wait> WaitFirstInThree()
        {
            yield return Wait("Wait First In Three",
                Wait<string, string>(Method7, "Method 7"),
                Wait<string, string>(Method8, "Method 8"),
                Wait<string, string>(Method9, "Method 9")
            ).MatchAny();
            await Task.Delay(100);
            Console.WriteLine("WaitFirstInThree");
        }

        [PushCall("RequestAdded")]
        public string Method1(string input) => "RequestAdded Call";
        [PushCall("Method2")]
        public string Method2(string input) => "Method2 Call";
        [PushCall("Method3")] public string Method3(string input) => "Method3 Call";
        [PushCall("Method4")] public string Method4(string input) => "Method4 Call";
        [PushCall("Method5")] public string Method5(string input) => "Method5 Call";
        [PushCall("Method6")] public string Method6(string input) => "Method6 Call";
        [PushCall("Method7")] public string Method7(string input) => "Method7 Call";
        [PushCall("Method8")] public string Method8(string input) => "Method8 Call";
        [PushCall("Method9")] public string Method9(string input) => "Method9 Call";
    }

}
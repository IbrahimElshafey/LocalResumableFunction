using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.Testing;

namespace Tests
{
    public class DeleteUnusedTemplateSiblingsTest
    {
        [Fact]
        public async Task DeleteUnusedTemplateSiblings_Test()
        {
            using var test = new TestShell(nameof(DeleteUnusedTemplateSiblings_Test), typeof(Test));
            await test.ScanTypes();
            var timeWaitId = await test.RoundCheck(0, 0, 0);

            var instance = new Test();
            instance.Method1("1");
            instance.Method1("1");
            instance.Method1("1");
            instance.Method2("2");
            instance.Method2("2");
            instance.Method2("2");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
            instance.Method1("#");
            instance.Method2("#");
        }

        public class Test : ResumableFunctionsContainer
        {
            private int _dynamicProp;

            public Test()
            {
                _dynamicProp = Random.Shared.Next(2, 100);
            }

            [ResumableFunctionEntryPoint("DeleteUnusedTemplateSiblingsTest")]
            public async IAsyncEnumerable<Wait> ThreeMethodsSequence()
            {

                yield return
                    Wait<string, string>(Method1, "M1")
                    .MatchIf((input, output) => LocalValue(_dynamicProp) > 1);
                yield return Wait<string, string>(Method2, "M2")
                    .MatchIf((input, output) => LocalValue(_dynamicProp) > 1);
                await Task.Delay(100);
            }

            [PushCall("Method1")] public string Method1(string input) => input + "M1";
            [PushCall("Method2")] public string Method2(string input) => input + "M2";
        }
    }
}
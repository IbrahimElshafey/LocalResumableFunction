using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace ResumableFunctionScanner;

internal class Program
{
    public static async Task Main(string[] args)
    {
        RerwiteSetDataTest();
        //await new Scanner().Start();
    }

    private static void RerwiteSetDataTest()
    {
        var test = new Test();
        var wait = test.GetWait();
        wait.SetDataExpression = new RewriteSetDataExpression(wait).Result;
    }
}
internal class Test : ResumableFunctionLocal
{
    public int Result { get; set; }

    [ResumableFunctionEntryPoint]
    internal MethodWait GetWait()
    {
        var methodWait = new MethodWait<int, int>(Method)
                     .SetData((input, output) => Result == output);
        methodWait.CurrntFunction = this;
        return methodWait;
    }

    [WaitMethod]
    internal int Method(int input)
    {
        return 10;
    }
}
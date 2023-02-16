using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using System.Linq.Expressions;

namespace ResumableFunctionScanner;

internal class Program
{
    public static async Task Main(string[] args)
    {
#if DEBUG
        File.Delete($"{AppContext.BaseDirectory}LocalResumableFunctionsData.db");
#endif
        await new Scanner().Start();
        //RerwiteSetDataTest();
    }

    private static void RerwiteSetDataTest()
    {
        //var test = new Test();
        //var wait = test.GetWait();
        //wait.SetDataExpression = new RewriteSetDataExpression(wait).Result;
        //Test test1 = new Test();
        //wait.SetDataExpression.Compile().DynamicInvoke(10,11, test1);

    }
}
internal class Test : ResumableFunctionLocal
{
    public int Decision { get; set; }
    public int ProjectId { get; set; }

    [ResumableFunctionEntryPoint]
    internal MethodWait GetWait()
    {
        var methodWait =
            new MethodWait<int, int>(Method)
            .SetData((projectId, decision) => Decision == decision && projectId == ProjectId);
        methodWait.CurrntFunction = this;
        return methodWait;
    }

    [WaitMethod]
    internal int Method(int input)
    {
        return 10;
    }
}
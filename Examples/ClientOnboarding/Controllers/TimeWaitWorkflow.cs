using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ClientOnboarding.Controllers;

public class TimeWaitWorkflow : ResumableFunction
{
    [ResumableFunctionEntryPoint("TestTimeWait")]
    public async IAsyncEnumerable<Wait> TestTimeWaitAtStart()
    {

        yield return Wait(TimeSpan.FromDays(1), "one day");
        Console.WriteLine("Time wait at start matched.");
    }

    public int Method1(int input) => 10;
}
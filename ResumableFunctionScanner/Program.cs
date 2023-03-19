using ResumableFunctions.Core;

namespace ResumableFunctionScanner;

internal class Program
{
    public static async Task Main(string[] args)
    {
        await new Scanner().Start();
    }
}
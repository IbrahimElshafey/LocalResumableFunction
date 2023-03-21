using ResumableFunctions.Core;

namespace ResumableFunctionScanner;

internal class Program
{
    public static async Task Main(string[] args)
    {
        //todo:Scanner should create file for [ExternalWaitMethod] matched to faclitate using 
        await new Scanner(new ResumableFunctionHandler()).Start();
    }
}
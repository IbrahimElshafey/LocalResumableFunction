namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitsProcessor
    {
        Task ProcessFunctionExpectedWaitMatches(int functionId, int pushedCallId);
    }
}
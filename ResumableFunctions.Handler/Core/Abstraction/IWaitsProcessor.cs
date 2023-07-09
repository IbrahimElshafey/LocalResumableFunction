namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitsProcessor
    {
        Task ProcessFunctionExpectedWaitMatches(int functionId, int pushedCallId);
        Task ProcessFunctionExpectedWaitMatchesV2(int functionId, int pushedCallId, int methodGroupId);
    }
}
namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitsProcessor
    {
        Task ProcessFunctionExpectedMatchedWaits(int functionId, int pushedCallId, int methodGroupId);
    }
}
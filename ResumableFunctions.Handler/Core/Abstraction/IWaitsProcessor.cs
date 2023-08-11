namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitsProcessor
    {
        Task ProcessFunctionExpectedMatchedWaits(int functionId, long pushedCallId, int methodGroupId);
    }
}
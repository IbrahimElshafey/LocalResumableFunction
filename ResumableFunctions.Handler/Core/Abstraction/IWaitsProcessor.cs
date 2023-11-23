namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitsProcessor
    {
        Task ProcessFunctionExpectedWaits(int functionId, long pushedCallId, int methodGroupId, DateTime pushedCallDate);
    }
}
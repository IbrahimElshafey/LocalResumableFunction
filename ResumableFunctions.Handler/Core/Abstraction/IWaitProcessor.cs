namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitProcessor
    {
        Task ProcessFunctionExpectedMatches(int functionId, int pushedCallId);
    }
}
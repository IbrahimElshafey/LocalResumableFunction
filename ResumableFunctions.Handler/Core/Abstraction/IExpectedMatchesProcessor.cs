namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IExpectedMatchesProcessor
    {
        Task ProcessFunctionExpectedMatches(long functionId, long pushedCallId);
    }
}
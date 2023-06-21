namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IExpectedMatchesProcessor
    {
        Task ProcessFunctionExpectedMatches(int functionId, int pushedCallId);
    }
}
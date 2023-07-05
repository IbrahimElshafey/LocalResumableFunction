namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallProcessor
    {
        Task InitialProcessPushedCall(int pushedCallId, string methodUrn);
        Task ServiceProcessPushedCall(int pushedCallId, string methodUrn);
    }
}
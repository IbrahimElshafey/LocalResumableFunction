using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallProcessor
    {
        Task InitialProcessPushedCall(long pushedCallId, string methodUrn);
        Task ServiceProcessPushedCall(CallServiceImapction service);
    }
}
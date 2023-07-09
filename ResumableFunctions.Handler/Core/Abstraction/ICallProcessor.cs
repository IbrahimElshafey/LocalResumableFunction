using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallProcessor
    {
        Task InitialProcessPushedCall(int pushedCallId, string methodUrn);
        Task ServiceProcessPushedCall(AffectedService service);
    }
}
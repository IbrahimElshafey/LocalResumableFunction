using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallProcessor
    {
        Task InitialProcessPushedCallV2(int pushedCallId, string methodUrn);
        Task InitialProcessPushedCall(int pushedCallId, string methodUrn);
        Task ServiceProcessPushedCall(int pushedCallId, string methodUrn);
        Task ServiceProcessPushedCallV2(AffectedService service);
    }
}
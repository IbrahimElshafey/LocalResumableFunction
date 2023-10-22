using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IServiceQueue
    {
        Task RouteCallToAffectedServices(long pushedCallId, string methodUrn);
        Task ServiceProcessPushedCall(CallEffection service);
        Task ProcessCallLocally(long pushedCallId, string methodUrn);
        Task EnqueueCallEffection(CallEffection callImapction);
    }
}
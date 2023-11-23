using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IServiceQueue
    {
        Task RouteCallToAffectedServices(long pushedCallId, DateTime puhsedCallDate, string methodUrn);
        Task ServiceProcessPushedCall(CallEffection service);
        Task ProcessCallLocally(long pushedCallId, string methodUrn, DateTime puhsedCallDate);
        Task EnqueueCallEffection(CallEffection callImapction);
    }
}
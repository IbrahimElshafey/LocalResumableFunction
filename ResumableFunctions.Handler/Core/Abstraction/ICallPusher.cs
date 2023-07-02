using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallPusher
    {
        Task<long> PushCall(PushedCall pushedCall);
        Task<long> PushExternalCall(PushedCall pushedCall, string serviceName);
    }
}
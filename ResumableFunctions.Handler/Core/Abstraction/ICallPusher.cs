using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ICallPusher
    {
        Task<int> PushCall(PushedCall pushedCall);
        Task<int>  PushExternalCall(PushedCall pushedCall, string serviceName);
    }
}
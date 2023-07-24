using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IServiceQueue
    {
        Task EnqueueCallImpaction(CallServiceImapction callImapction);
    }
}
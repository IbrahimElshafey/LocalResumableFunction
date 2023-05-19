using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface ISaveWaitHandler
    {
        Task<bool> SaveWaitRequestToDb(Wait newWait);
    }
}
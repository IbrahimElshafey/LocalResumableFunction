using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler
{
    public interface ISaveWaitHandler
    {
        Task<bool> SaveWaitRequestToDb(Wait newWait);
    }
}
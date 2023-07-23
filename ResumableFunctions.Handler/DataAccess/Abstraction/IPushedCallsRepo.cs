using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IPushedCallsRepo
{
    Task<PushedCall> GetById(int pushedCallId);
    void Push(PushedCall pushedCall);
}
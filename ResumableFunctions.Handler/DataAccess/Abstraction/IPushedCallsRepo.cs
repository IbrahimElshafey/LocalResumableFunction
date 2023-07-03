using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IPushedCallsRepo
{
    Task<PushedCall> GetById(long pushedCallId);
    void Add(PushedCall pushedCall);
}
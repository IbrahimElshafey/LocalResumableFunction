using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IRuntimeClosureRepo
    {
        Task<RuntimeClosure> GetRuntimeClosure(Guid guid);
    }
}
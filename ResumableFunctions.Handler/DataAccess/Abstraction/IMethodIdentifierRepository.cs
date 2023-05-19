using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IMethodIdentifierRepository
    {
        Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData, int? serviceId);
        Task AddWaitMethodIdentifier(MethodData methodData, int service);
        Task<ResumableFunctionIdentifier> GetResumableFunction(int id);
        Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData);
        Task<WaitMethodIdentifier> GetWaitMethod(MethodWait methodWait);
    }
}
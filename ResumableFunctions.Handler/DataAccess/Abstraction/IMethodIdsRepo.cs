using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IMethodIdsRepo
    {
        Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData);
        Task AddWaitMethodIdentifier(MethodData methodData);
        Task<ResumableFunctionIdentifier> GetResumableFunction(long id);
        Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData);
        Task<(long MethodId, long GroupId)> GetId(MethodWait methodWait);
    }
}
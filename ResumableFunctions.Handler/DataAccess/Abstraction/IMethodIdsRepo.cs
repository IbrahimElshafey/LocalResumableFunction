using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IMethodIdsRepo
    {
        Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData);
        Task AddWaitMethodIdentifier(MethodData methodData);
        Task<ResumableFunctionIdentifier> GetResumableFunction(int id);
        Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData);
        Task<(int MethodId, int GroupId)> GetId(MethodWait methodWait);
    }
}
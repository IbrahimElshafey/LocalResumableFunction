using ResumableFunctions.Data.Abstraction.Entities;
using System.Threading.Tasks;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IMethodIdsRepo
    {
        Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData);
        Task AddMethodIdentifier(MethodData methodData);
        Task<ResumableFunctionIdentifier> GetResumableFunction(int id);
        Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData);
        Task<(int MethodId, int GroupId)> GetId(MethodWaitEntity methodWait);
        Task<WaitMethodIdentifier> GetMethodIdentifierById(int? methodWaitMethodToWaitId);
        Task<bool> CanPublishFromExternal(string methodUrn);
    }
}
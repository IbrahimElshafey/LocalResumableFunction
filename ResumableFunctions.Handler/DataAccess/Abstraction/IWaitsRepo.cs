using ResumableFunctions.Handler.InOuts;
using System.Linq.CompilerServices.TypeSystem;
using System.Linq.Expressions;
using System.Reflection;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitsRepo
{
    Task CancelFunctionWaits(int requestedByFunctionId, int functionStateId);
    Task CancelOpenedWaitsForState(int stateId);
    Task CancelSubWaits(int parentId, int pushedCallId);
    Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait);
    Task<Wait> GetWaitParent(Wait wait);
    Task<List<int>> GetMatchedFunctionsForCall(int pushedCallId, string methodUrn);
    Task<List<ServiceData>> GetAffectedServicesForCall(string methodUrn);
    Task RemoveFirstWaitIfExist(int methodIdentifierId);
    Task<bool> SaveWait(Wait newWait);
    Task<MethodWait> GetMethodWait(int waitId, params Expression<Func<MethodWait, object>>[] includes);
    Task<MethodInfo> GetRequestedByMethodInfo(int rootId);
}
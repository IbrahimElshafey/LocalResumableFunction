using ResumableFunctions.Handler.InOuts;
using System.Linq.CompilerServices.TypeSystem;
using System.Linq.Expressions;
using System.Reflection;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitsRepo
{
    Task CancelFunctionWaits(long requestedByFunctionId, long functionStateId);
    Task CancelOpenedWaitsForState(long stateId);
    Task CancelSubWaits(long parentId, long pushedCallId);
    Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait);
    Task<Wait> GetWaitParent(Wait wait);
    Task<List<long>> GetMatchedFunctionsForCall(long pushedCallId, string methodUrn);
    Task<List<ServiceData>> GetAffectedServicesForCall(string methodUrn);
    Task RemoveFirstWaitIfExist(long methodIdentifierId);
    Task<bool> SaveWait(Wait newWait);
    Task<MethodWait> GetMethodWait(long waitId, params Expression<Func<MethodWait, object>>[] includes);
    Task<MethodInfo> GetRequestedByMethodInfo(long rootId);
}
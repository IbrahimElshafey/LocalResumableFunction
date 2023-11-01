using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitsRepo
{
    Task CancelFunctionPendingWaits(int requestedByFunctionId, int functionStateId);
    Task CancelOpenedWaitsForState(int stateId);
    Task CancelSubWaits(long parentId, long pushedCallId);
    Task<WaitEntity> GetOldWaitForReplay(ReplayRequest replayWait);
    Task<WaitEntity> GetWaitParent(WaitEntity wait);
    Task<List<CallEffection>> GetAffectedServicesAndFunctions(string methodUrn);
    Task<CallEffection> GetCallEffectionInCurrentService(string methodUrn);
    Task RemoveFirstWaitIfExist(int methodIdentifierId);
    Task<bool> SaveWait(WaitEntity newWait);
    Task<MethodWaitEntity> GetMethodWait(int waitId, params Expression<Func<MethodWaitEntity, object>>[] includes);
    Task<MethodInfo> GetMethodInfoForRf(int waitId);
    Task<List<MethodWaitEntity>> GetPendingWaitsForTemplate(WaitTemplate template, string mandatoryPart, params Expression<Func<MethodWaitEntity, object>>[] includes);

    //Task PropagateClosureIfChanged(WaitEntity wait);
}
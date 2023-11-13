using ResumableFunctions.Handler.DataAccess.InOuts;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitsRepo
{
    //Hint: I didn't use the best practice here , repo should contains query about data and names should not look like a business calls
    Task CancelFunctionPendingWaits(WaitEntity waitForReplayDb);
    Task CancelOpenedWaitsForState(int stateId);
    Task CancelSubWaits(long parentId, long pushedCallId);
    Task<WaitEntity> GetWaitParent(WaitEntity wait);
    Task<List<CallEffection>> GetAffectedServicesAndFunctions(string methodUrn);
    Task<CallEffection> GetCallEffectionInCurrentService(string methodUrn);
    Task RemoveFirstWaitIfExist(int methodIdentifierId);
    Task<bool> SaveWait(WaitEntity newWait);
    Task<MethodWaitEntity> GetMethodWait(long waitId, params Expression<Func<MethodWaitEntity, object>>[] includes);
    Task<MethodInfo> GetMethodInfoForRf(long waitId);
    Task<List<MethodWaitEntity>> GetPendingWaitsForTemplate(int templateId, string mandatoryPart, params Expression<Func<MethodWaitEntity, object>>[] includes);

    Task<List<PendingWaitData>> GetPendingWaitsData(int methodGroupId, int functionId);
}
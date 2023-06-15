using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IWaitsRepo
    {
        Task AddWait(Wait wait);
        Task CancelFunctionWaits(int requestedByFunctionId, int functionStateId);
        Task CancelOpenedWaitsForState(int stateId);
        Task CancelSubWaits(int parentId);
        Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait);
        Task<Wait> GetWaitParent(Wait wait);
        Task<List<WaitId>> GetWaitsIdsForMethodCall(int pushedCallId,string methodUrn);
        Task<List<ServiceData>> GetAffectedServicesForCall(string methodUrn);
        Task RemoveFirstWaitIfExist(int methodIdentifierId);

        Task<bool> SaveWaitRequestToDb(Wait newWait);
        Task<MethodWait> GetTimeWait(string timeWaitId);
    }
}
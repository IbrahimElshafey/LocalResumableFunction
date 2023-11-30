namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface ILockStateRepo
    {
        Task<int> AddLockState(string name);
        Task<bool> RemoveLockState(int id);
        Task<bool> AreLocksExist();
        Task ResetServiceLockStates();
    }
}
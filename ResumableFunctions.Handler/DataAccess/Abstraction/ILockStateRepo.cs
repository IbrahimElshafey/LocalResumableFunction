namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    //todo:must be reviewed
    public interface ILockStateRepo
    {
        Task<int> AddLockState(string name);
        Task<bool> RemoveLockState(int id);
        Task<bool> AreLocksExist();
        Task ResetServiceLockStates();
    }
}
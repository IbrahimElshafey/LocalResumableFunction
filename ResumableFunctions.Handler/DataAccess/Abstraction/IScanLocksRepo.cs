namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    //todo:must be reviewed
    public interface IScanLocksRepo
    {
        /// <summary>
        /// Add rows in locks table for every scan process
        /// </summary>
        Task<int> AddLock(string name);
        Task<bool> RemoveLock(int id);
        Task<bool> AreLocksExist();
        Task ResetServiceLocks();
    }
}
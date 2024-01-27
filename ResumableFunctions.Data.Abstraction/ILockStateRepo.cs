using System.Threading.Tasks;

namespace ResumableFunctions.Data.Abstraction
{
    public interface ILockStateRepo
    {
        Task<int> AddLockState(string name);
        Task<bool> RemoveLockState(int id);
        Task<bool> AreLocksExist();
        Task ResetServiceLockStates();
    }
}
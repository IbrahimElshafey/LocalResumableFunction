using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Core
{
    internal class RecycleBinService : IRecycleBinService
    {
        public Task RecycleFunction(int functionInstanceId)
        {
            return Task.CompletedTask;
        }
    }
}

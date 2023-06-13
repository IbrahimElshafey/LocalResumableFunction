namespace ResumableFunctions.Handler.Helpers
{
    public class ConcurrencyControl
    {
        public Task<bool> AcquireLock(string entityName,int entityId)
        {
            return Task.FromResult(true);
        }
        public Task<bool> ReleaseLock(string entityName, int entityId)
        {
            return Task.FromResult(true);
        }
    }
}

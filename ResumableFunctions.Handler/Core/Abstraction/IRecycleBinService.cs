namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IRecycleBinService
    {
        Task RecycleFunction(long functionInstanceId);
    }
}

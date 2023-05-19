namespace ResumableFunctions.Handler
{
    public interface IWaitProcessor
    {
        Task Run(int mehtodWaitId, int pushedCallId);
    }
}
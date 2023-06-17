namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitProcessor
    {
        Task ProcessWait(int methodWaitId, int pushedCallId);
    }
}
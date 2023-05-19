namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IWaitProcessor
    {
        Task RequestProcessing(int mehtodWaitId, int pushedCallId);
    }
}
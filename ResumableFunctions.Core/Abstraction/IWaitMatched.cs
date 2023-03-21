namespace ResumableFunctions.Core.Abstraction
{
    public interface IWaitMatchedHandler
    {
        void WaitMatched(int waitId, int pushedMethodId);
    }
}

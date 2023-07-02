using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IFirstWaitProcessor
    {
        Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait);
        Task RegisterFirstWait(long functionId);
    }
}
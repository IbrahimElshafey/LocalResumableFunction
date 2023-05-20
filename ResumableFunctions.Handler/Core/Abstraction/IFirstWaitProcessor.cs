using ResumableFunctions.Handler.InOuts;
using System.Reflection;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IFirstWaitProcessor
    {
        Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait);
        Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist);
        Task RegisterFirstWait(int functionId);
        Task DeactivateFirstWait(int functionId);
    }
}
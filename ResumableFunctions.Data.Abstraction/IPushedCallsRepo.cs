using ResumableFunctions.Data.Abstraction.Entities;
using System.Threading.Tasks;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IPushedCallsRepo
    {
        Task<PushedCall> GetById(long pushedCallId);
        Task Push(PushedCall pushedCall);
        Task<bool> PushedCallMatchedForFunctionBefore(long pushedCallId, int rootFunctionId);
    }
}
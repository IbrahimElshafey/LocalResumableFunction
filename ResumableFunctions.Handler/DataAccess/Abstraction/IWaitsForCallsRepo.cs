using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IWaitsForCallsRepo
{
    Task<List<WaitForCall>> GetWaitsForCall(int pushedCallId, int functionId);
}
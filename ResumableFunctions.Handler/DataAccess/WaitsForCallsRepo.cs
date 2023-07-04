using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitsForCallsRepo : IWaitsForCallsRepo
{
    private readonly WaitsDataContext _context;

    public WaitsForCallsRepo(WaitsDataContext context)
    {
        _context = context;
    }
    public async Task<List<WaitForCall>> GetWaitsForCall(int pushedCallId, int functionId)
    {
        return
            await _context
                .WaitsForCalls
                .Where(x =>
                    x.PushedCallId == pushedCallId &&
                    x.FunctionId == functionId &&
                    (x.MatchStatus == MatchStatus.PartiallyMatched ||
                     x.InstanceUpdateStatus == InstanceUpdateStatus.UpdateFailed))
                .ToListAsync();
    }
}
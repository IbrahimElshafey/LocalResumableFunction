using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitProcessingRecordsRepo : IWaitProcessingRecordsRepo
{
    private readonly WaitsDataContext _context;

    public WaitProcessingRecordsRepo(WaitsDataContext context)
    {
        _context = context;
    }

    public WaitProcessingRecord Add(WaitProcessingRecord waitProcessingRecord)
    {
        _context.WaitsForCalls.Add(waitProcessingRecord);
        return waitProcessingRecord;
    }

    public async Task<List<WaitProcessingRecord>> GetWaitsForCall(int pushedCallId, int functionId)
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
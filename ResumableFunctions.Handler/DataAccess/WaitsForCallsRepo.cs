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

    public async Task<WaitProcessingRecord> Add(WaitProcessingRecord waitProcessingRecord)
    {
        _context.WaitProcessingRecords.Add(waitProcessingRecord);
        await _context.SaveChangesAsync();
        return waitProcessingRecord;
    }
}
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess;

internal class PushedCallsRepo : IPushedCallsRepo
{
    private readonly WaitsDataContext _context;

    public PushedCallsRepo(WaitsDataContext context)
    {
        _context = context;
    }
    public async Task<PushedCall> GetById(long pushedCallId)
    {
        return await _context
            .PushedCalls
            .FindAsync(pushedCallId);
    }

    public Task Push(PushedCall pushedCall)
    {
        _context.PushedCalls.Add(pushedCall);
        return Task.CompletedTask;
    }

    public async Task<bool> HasMatchBeforeForInstance(long pushedCallId, int functionStateId)
    {
        return await _context.
            MethodWaits.
            AsNoTracking().
            Where(x =>
                x.Status == InOuts.WaitStatus.Completed &&
                x.CallId == pushedCallId &&
                x.FunctionStateId == functionStateId)
            .AnyAsync();
    }
}
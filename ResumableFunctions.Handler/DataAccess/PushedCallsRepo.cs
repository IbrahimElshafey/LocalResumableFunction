using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

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

    public void Add(PushedCall pushedCall)
    {
        _context.PushedCalls.Add(pushedCall);
    }
}
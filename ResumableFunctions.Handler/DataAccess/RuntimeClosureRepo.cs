using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess;

internal class RuntimeClosureRepo : IRuntimeClosureRepo
{
    private readonly WaitsDataContext _context;

    public RuntimeClosureRepo(WaitsDataContext context)
    {
        _context = context;
    }

    public async Task<RuntimeClosure> GetRuntimeClosure(Guid guid)
    {
        return await _context.RuntimeClosures.FindAsync(guid);
    }
}

using ResumableFunctions.Handler.DataAccess.Abstraction;

namespace ResumableFunctions.Handler.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private readonly FunctionDataContext _context;

    public UnitOfWork(FunctionDataContext context) =>
        _context = context;

    public async Task<bool> Commit()
    {
        var success = await _context.SaveChangesAsync() > 0;

        // Possibility to dispatch domain events, etc

        return success;
    }

    public void Dispose() =>
        _context.Dispose();

    public Task Rollback()
    {
        // Rollback anything, if necessary
        return Task.CompletedTask;
    }
}

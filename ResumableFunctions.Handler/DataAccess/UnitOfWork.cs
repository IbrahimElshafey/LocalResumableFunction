using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class UnitOfWork : IUnitOfWork
{
    private readonly WaitsDataContext _context;

    public UnitOfWork(WaitsDataContext context) =>
        _context = context;

    public async Task<bool> SaveChangesAsync()
    {
        var success = await _context.SaveChangesAsync() > 0;

        // Possibility to dispatch domain events, etc

        return success;
    }

    public void Dispose() => _context.Dispose();

    public Task Rollback()
    {
        // Rollback anything, if necessary
        return Task.CompletedTask;
    }

    public void MarkEntityAsModified(object entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
    }
}

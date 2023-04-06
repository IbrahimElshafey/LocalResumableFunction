namespace ResumableFunctions.Handler.Data;

internal class RepositoryBase
{
    protected readonly FunctionDataContext _context;

    public RepositoryBase(FunctionDataContext ctx)
    {
        _context = ctx;
    }
}
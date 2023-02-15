namespace LocalResumableFunction.Data;

internal class RepositoryBase
{
    protected readonly FunctionDataContext Context;

    public RepositoryBase(FunctionDataContext ctx) => Context = ctx;
}
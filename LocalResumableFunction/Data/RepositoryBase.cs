namespace LocalResumableFunction.Data;

internal class RepositoryBase
{
    protected readonly EngineDataContext Context;

    public RepositoryBase(EngineDataContext ctx) => Context = ctx;
}
namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        Task<bool> Commit();
        Task Rollback();
    }
}

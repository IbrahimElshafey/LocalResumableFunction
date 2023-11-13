namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        Task<bool> CommitAsync();
        Task Rollback();
        void MarkEntityAsModified(object entity);
    }
}

using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        Task<bool> SaveChangesAsync();
        Task Rollback();
        void MarkEntityAsModified(object entity);
    }
}

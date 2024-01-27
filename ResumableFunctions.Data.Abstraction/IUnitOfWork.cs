using System.Threading.Tasks;
using System;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        Task<bool> CommitAsync();
        Task Rollback();
        void MarkEntityAsModified(object entity);
    }
}

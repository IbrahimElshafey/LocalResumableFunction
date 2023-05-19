using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        Task<bool> Commit();
        Task Rollback();
    }
}

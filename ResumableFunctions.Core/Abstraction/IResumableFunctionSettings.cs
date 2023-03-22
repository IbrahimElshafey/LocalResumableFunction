using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core.Abstraction
{
    public interface IResumableFunctionSettings
    {
        public IGlobalConfiguration HangFireConfig { get;  }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
    }
}

using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core.InOuts
{
    public interface IResumableFunctionSettings
    {
        public IGlobalConfiguration HangFireConfig { get; }
        public DbContextOptionsBuilder WaitsDbConfig { get; }
        public string ServiceUrl { get; }
        public string[] DllsToScan { get; set; }
    }
}

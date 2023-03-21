using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core.Abstraction
{
    public class ResumableFunctionSettings
    {
        public Action<IGlobalConfiguration> HangFireConfig { get; }
        public Action<DbContextOptionsBuilder> WaitsDbConfig { get; }
    }
}

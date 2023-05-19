using ResumableFunctions.Handler.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Core
{
    internal class RecycleBinService : IRecycleBinService
    {
        public Task RecycleFunction(int functionInstanceId)
        {
            return Task.CompletedTask;
        }
    }
}

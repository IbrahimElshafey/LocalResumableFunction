using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Core.Abstraction
{
    public interface IRecycleBinService
    {
        Task RecycleFunction(int functionInstanceId);
    }
}

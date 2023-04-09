using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    internal interface ConcurrencyControl
    {
        public ILock AcquireProcssLock(string lockName);
        public ILock AcquireInterServicesLock(string lockName);
    }

    public interface ILock : IDisposable
    {

    }
}

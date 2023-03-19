using ResumableFunctions.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Core.Helpers
{
    internal class LocalRegisteredMethods
    {
        [WaitMethod]
        internal string TimeWait(string timeWaitId)
        {
            return timeWaitId;
        }
    }
}

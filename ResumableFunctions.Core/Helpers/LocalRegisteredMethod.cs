using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Core.Helpers
{
    public class LocalRegisteredMethods
    {
        [WaitMethod]
        public string TimeWait(string timeWaitId)
        {
            return nameof(MethodWait.RefineMatchModifier) + timeWaitId;
        }
    }
}

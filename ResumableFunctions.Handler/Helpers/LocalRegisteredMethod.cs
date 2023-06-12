using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    public class LocalRegisteredMethods
    {
        [PushCall("LocalRegisteredMethods.TimeWait")]
        public string TimeWait(string timeWaitId)
        {
            return nameof(MethodWait.MandatoryPart) + timeWaitId;
        }
    }
}

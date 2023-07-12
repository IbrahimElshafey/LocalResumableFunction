using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using System.ComponentModel;

namespace ResumableFunctions.Handler.Helpers
{
    public class LocalRegisteredMethods
    {
        [PushCall(Constants.TimeWaitMethodUrn, IsLocalOnly = true, CanPublishFromExternal = false)]
        [DisplayName("{0}")]
        public bool TimeWait(TimeWaitInput timeWaitInput)
        {
            return true;
        }
    }
}

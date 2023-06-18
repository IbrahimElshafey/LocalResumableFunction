using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Helpers
{
    public class LocalRegisteredMethods
    {
        [PushCall("LocalRegisteredMethods.TimeWait")]
        public bool TimeWait(TimeWaitInput timeWaitInput)
        {
            return true;
        }
    }
}

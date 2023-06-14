using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Helpers
{
    public class LocalRegisteredMethods
    {
        [PushCall("LocalRegisteredMethods.TimeWait")]
        public string TimeWait(string timeWaitId)
        {
            //todo: special handle for time wait push
            return nameof(MethodWait.MandatoryPart) + timeWaitId;
        }
    }
}

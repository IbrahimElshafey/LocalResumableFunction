using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Helpers
{
    public class LocalRegisteredMethods
    {
        private readonly IWaitProcessor _waitProcessor;

        public LocalRegisteredMethods(IWaitProcessor waitProcessor)
        {
            _waitProcessor = waitProcessor;
        }
        [PushCall("LocalRegisteredMethods.TimeWait")]
        public bool TimeWait(string timeWaitId)
        {
            _waitProcessor.ProcessTimeWaitMatched(timeWaitId).Wait();
            return true;
        }
    }
}

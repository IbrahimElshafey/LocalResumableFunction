using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctionsReceiver")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsReceiverController : ControllerBase
    {
        public IResumableFunctionsReceiver WaitMatchedHandler { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public ResumableFunctionsReceiverController(IResumableFunctionsReceiver waitMatched, IBackgroundJobClient backgroundJobClient)
        {
            BackgroundJobClient = backgroundJobClient;
            WaitMatchedHandler = waitMatched;
        }


        [HttpGet(nameof(WaitMatched))]
        public int WaitMatched(int waitId, int pushedMethodId)
        {
            BackgroundJobClient.Enqueue(() => WaitMatchedHandler.WaitMatched(waitId, pushedMethodId));
            return 0;
        }

        [HttpGet(nameof(PushExternal))]
        public int PushExternal(PushedMethod pushedMethod)
        {
            BackgroundJobClient.Enqueue(() => WaitMatchedHandler.ProcessPushedMethod(pushedMethod));
            return 0;
        }
    }
}
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResumableFunctions.Core;
using ResumableFunctions.Core.Abstraction;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctionReceiver")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionReceiverController : ControllerBase
    {
        public IWaitMatchedHandler WaitMatchedHandler { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public ResumableFunctionReceiverController(IWaitMatchedHandler waitMatched, IBackgroundJobClient backgroundJobClient)
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
    }
}
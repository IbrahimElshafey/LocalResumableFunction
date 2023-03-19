using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResumableFunctions.Core;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctionReceiver")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionReceiverController : ControllerBase
    {
        public IBackgroundTaskQueue BackgroundTaskQueue { get; }
        public ResumableFunctionReceiverController(IBackgroundTaskQueue backgroundTaskQueue)
        {
            BackgroundTaskQueue = backgroundTaskQueue;
        }

        
        [HttpGet(nameof(WaitMatched))]
        public int WaitMatched(int waitId, int pushedMethodId)
        {
           Task.Run(async() => await BackgroundTaskQueue.QueueBackgroundWorkItemAsync())
            return 0;
        }
    }
}
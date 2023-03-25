using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResumableFunctions.Core;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctionsReceiver")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsReceiverController : ControllerBase
    {
        public ResumableFunctionHandler ResumableFunctionHandler { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public ResumableFunctionsReceiverController(ResumableFunctionHandler handler, IBackgroundJobClient backgroundJobClient)
        {
            BackgroundJobClient = backgroundJobClient;
            ResumableFunctionHandler = handler;
        }


        [HttpGet(nameof(ProcessMatchedWait))]
        public int ProcessMatchedWait(int waitId, int pushedMethodId)
        {
            BackgroundJobClient.Enqueue(() => ResumableFunctionHandler.ProcessMatchedWait(waitId, pushedMethodId));
            return 0;
        }

        //[HttpGet(nameof(PushExternal))]
        //public int PushExternal(PushedMethod pushedMethod)
        //{
        //    //BackgroundJobClient.Enqueue(() => ResumableFunctionHandler.ProcessPushedMethod(pushedMethod));
        //    return 0;
        //}
    }
}
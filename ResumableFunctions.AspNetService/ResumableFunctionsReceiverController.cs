using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResumableFunctions.Core;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctions")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsController : ControllerBase
    {
        public ResumableFunctionHandler ResumableFunctionHandler { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public ResumableFunctionsController(ResumableFunctionHandler handler, IBackgroundJobClient backgroundJobClient)
        {
            BackgroundJobClient = backgroundJobClient;
            ResumableFunctionHandler = handler;
        }


        [HttpGet(nameof(ProcessMatchedWait))]
        public int ProcessMatchedWait(int waitId, int pushedMethodId)
        {
            BackgroundJobClient.Enqueue(() => ResumableFunctionHandler.ProcessExternalMatchedWait(waitId, pushedMethodId));
            return 0;
        }

        //todo:PushExternal
        //[HttpGet(nameof(PushExternal))]
        //public int PushExternal(PushedMethod pushedMethod)
        //{
        //    BackgroundJobClient.Enqueue(() => ResumableFunctionHandler.ProcessPushedMethod(pushedMethod));
        //    return 0;
        //}

        //todo:CheckMethod/s exist
        //todo:force rescan
        //
    }
}
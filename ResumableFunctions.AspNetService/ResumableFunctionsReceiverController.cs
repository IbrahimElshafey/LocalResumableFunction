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
        private readonly ResumableFunctionHandler _resumableFunctionHandler;
        public readonly IBackgroundJobClient _backgroundJobClient;
        public ResumableFunctionsController(ResumableFunctionHandler handler, IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
            _resumableFunctionHandler = handler;
        }


        [HttpGet(nameof(ProcessMatchedWait))]
        public int ProcessMatchedWait(int waitId, int pushedMethodId)
        {
            _backgroundJobClient.Enqueue(() => _resumableFunctionHandler.ProcessExternalMatchedWait(waitId, pushedMethodId));
            return 0;
        }

        [HttpPost(nameof(WebHookMethod))]
        public async Task WebHookMethod(WebhookCall pushedMethod)
        {
            await 
                _resumableFunctionHandler.QueuePushedMethodProcessing(pushedMethod);
        }

        //todo:CheckMethod/s exist
        //todo:force rescan
        //
    }
}
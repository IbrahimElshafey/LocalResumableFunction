using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.InOuts;

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
        public int ProcessMatchedWait(int waitId, int pushedCallId)
        {
            _backgroundJobClient.Enqueue(() => _resumableFunctionHandler.ProcessMatchedWait(waitId, pushedCallId));
            return 0;
        }

        [HttpPost(nameof(ExternalCall))]
        public async Task ExternalCall(ExternalCallArgs externalCall)//todo:fix here
        {
            //var rawRequestBody = new StreamReader(Request.Body).ReadToEnd();
            //var externalCall = JsonConvert.DeserializeObject<ExternalCallArgs>(rawRequestBody);
            //todo:check if method wait exist and marked as external
            await
                _resumableFunctionHandler.QueuePushedCallProcessing(new PushedCall
                {
                    MethodData = new MethodData
                    {
                        MethodUrn = externalCall.MethodIdentifier
                    },
                    Input = externalCall.Input,
                    Output = externalCall.Output
                });
        }

        //todo:CheckMethod/s exist
        //todo:force rescan
        //todo:get readable expression for MatchExpression ,SetDataExpression and GroupMatchExpression
    }
}
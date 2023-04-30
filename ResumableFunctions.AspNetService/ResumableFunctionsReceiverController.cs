using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.InOuts;
using System.Text.Json;

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
        //public async Task ExternalCall(ExternalCallArgs externalCall)
        public async Task ExternalCall([FromBody]dynamic input)
        {
            var externalCall = 
                JsonConvert.DeserializeObject<ExternalCallArgs>((string)input.ToString());
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
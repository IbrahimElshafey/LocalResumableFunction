using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ResumableFunctionsController> _logger;

        public ResumableFunctionsController(
            ResumableFunctionHandler handler, 
            IBackgroundJobClient backgroundJobClient,
            ILogger<ResumableFunctionsController> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
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
            try
            {
                var externalCall =
                    JsonConvert.DeserializeObject<ExternalCallArgs>((string)input.ToString());
                if(externalCall == null) 
                    throw new ArgumentNullException(nameof(externalCall));
                if (await _resumableFunctionHandler.IsExternal(externalCall.MethodUrn) is false)
                    throw new Exception(
                        $"There is no method with URN [{externalCall?.MethodUrn}] that can be called from external in service [{externalCall?.ServiceName}].");
                await
                    _resumableFunctionHandler.QueuePushedCallProcessing(new PushedCall
                    {
                        MethodData = new MethodData
                        {
                            MethodUrn = externalCall.MethodUrn
                        },
                        Input = externalCall.Input,
                        Output = externalCall.Output
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when handle external method call.");
            }
            
        }

        //todo:CheckMethod/s exist
        //todo:force rescan
        //todo:get readable expression for MatchExpression ,SetDataExpression and GroupMatchExpression
    }
}
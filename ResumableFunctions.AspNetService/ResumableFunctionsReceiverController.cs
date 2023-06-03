using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.InOuts;
using System.Text.Json;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctions")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsController : ControllerBase
    {
        public readonly IWaitProcessor _waitProcessor;
        public readonly IPushedCallProcessor _pushedCallProcessor;
        public readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<ResumableFunctionsController> _logger;

        public ResumableFunctionsController(
            IBackgroundJobClient backgroundJobClient,
            ILogger<ResumableFunctionsController> logger,
            IPushedCallProcessor pushedCallProcessor,
            IWaitProcessor waitProcessor)
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
            _pushedCallProcessor = pushedCallProcessor;
            _waitProcessor = waitProcessor;
        }


        [HttpGet(nameof(ProcessMatchedWait))]
        public int ProcessMatchedWait(int waitId, int pushedCallId)
        {
            _backgroundJobClient.Enqueue(() => _waitProcessor.RequestProcessing(waitId, pushedCallId));
            return 0;
        }

        [HttpPost(nameof(ExternalCall))]
        //public async Task ExternalCall(ExternalCallArgs externalCall)
        public async Task ExternalCall([FromBody] dynamic input)
        {
            try
            {
                var externalCall =
                    JsonConvert.DeserializeObject<ExternalCallArgs>((string)input.ToString());
                if (externalCall == null)
                    throw new ArgumentNullException(nameof(externalCall));
                var pushedCall = new PushedCall
                {
                    MethodData = new MethodData
                    {
                        MethodUrn = externalCall.MethodUrn
                    },
                    Data = new()
                    {
                        Input = externalCall.Input,
                        Output = externalCall.Output
                    }
                };
                await _pushedCallProcessor.QueueExternalPushedCallProcessing(pushedCall, externalCall.ServiceName);
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.Buffers;
using System.Linq.CompilerServices.TypeSystem;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route($"api/ResumableFunctions")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsController : ControllerBase
    {
        public readonly ICallPusher _callPusher;
        public readonly ICallProcessor _callProcessor;
        private readonly ILogger<ResumableFunctionsController> _logger;

        public ResumableFunctionsController(
            ILogger<ResumableFunctionsController> logger,
            ICallPusher callPusher,
            ICallProcessor callProcessor)
        {
            _logger = logger;
            _callPusher = callPusher;
            _callProcessor = callProcessor;
        }


        [HttpGet(nameof(ServiceProcessPushedCall))]
        public async Task<int> ServiceProcessPushedCall(int pushedCallId, string methodUrn)
        {
            await _callProcessor.ServiceProcessPushedCall(pushedCallId, methodUrn);
            return 0;
        }

        [HttpPost(nameof(ExternalCall))]
        public async Task ExternalCall()
        {
            try
            {
                var x = await Request.BodyReader.ReadAsync();
                var bytes = x.Buffer.ToArray();
                var serialzer = new BinaryToObjectConverter();
                var externalCall = serialzer.ConvertToObject<ExternalCallArgs>(bytes);
                if (externalCall == null)
                    throw new ArgumentNullException(nameof(externalCall));
                var pushedCall = new PushedCall
                {
                    MethodData = externalCall.MethodData,
                    Data = new()
                    {
                        Input = externalCall.Input,
                        Output = externalCall.Output
                    }
                };
                pushedCall.MethodData.CanPublishFromExternal = true;
                await _callPusher.PushExternalCall(pushedCall, externalCall.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when handle external method call.");
            }
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.Buffers;
using System.Linq.CompilerServices.TypeSystem;
using System.Reflection;

namespace ResumableFunctions.AspNetService
{
    [ApiController]
    [Route(Constants.ResumableFunctionsControllerUrl)]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsController : ControllerBase
    {
        public readonly ICallPusher _callPusher;
        public readonly ICallProcessor _callProcessor;
        private readonly IBackgroundProcess _backgroundProcess;
        private readonly ILogger<ResumableFunctionsController> _logger;

        public ResumableFunctionsController(
            ILogger<ResumableFunctionsController> logger,
            ICallPusher callPusher,
            ICallProcessor callProcessor,
            IBackgroundProcess backgroundProcess)
        {
            _logger = logger;
            _callPusher = callPusher;
            _callProcessor = callProcessor;
            _backgroundProcess = backgroundProcess;
        }




        [HttpPost(Constants.ServiceProcessPushedCallAction)]
        public int ServiceProcessPushedCall(CallServiceImapction service)
        {
            _backgroundProcess.Enqueue(() => _callProcessor.ServiceProcessPushedCall(service));
            return 1;
        }

        [HttpPost(Constants.ExternalCallAction)]
        public async Task<int> ExternalCall()
        {
            var body = await Request.BodyReader.ReadAsync();
            var bytes = body.Buffer.ToArray();
            var serializer = new BinaryToObjectConverter();
            var externalCall = serializer.ConvertToObject<ExternalCallArgs>(bytes);
            return await ExternalCallJson(externalCall);
        }

        [HttpPost(Constants.ExternalCallAction + "Json")]
        public async Task<int> ExternalCallJson(ExternalCallArgs externalCall)
        {
            try
            {
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
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when handle external method call.");
                return -1;
            }
        }
    }
}
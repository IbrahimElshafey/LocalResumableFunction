﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Buffers;

namespace ResumableFunctions.MvcUi
{
    [ApiController]
    [Route(Constants.ResumableFunctionsControllerUrl)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ResumableFunctionsController : ControllerBase, IExternalCallReceiver
    {
        public readonly ICallPusher _callPusher;
        public readonly IServiceQueue _serviceQueue;
        private readonly IBackgroundProcess _backgroundProcess;
        private readonly ILogger<ResumableFunctionsController> _logger;

        public ResumableFunctionsController(
            ILogger<ResumableFunctionsController> logger,
            ICallPusher callPusher,
            IServiceQueue serviceQueue,
            IBackgroundProcess backgroundProcess)
        {
            _logger = logger;
            _callPusher = callPusher;
            _serviceQueue = serviceQueue;
            _backgroundProcess = backgroundProcess;
        }




        [HttpPost(Constants.ServiceProcessPushedCallAction)]
        public int ServiceProcessPushedCall(CallEffection callEffection)
        {
            _serviceQueue.ServiceProcessPushedCall(callEffection);
            return 1;
        }

        [HttpPost(Constants.ExternalCallAction)]
        public async Task<int> ExternalCall()
        {
            var body = await Request.BodyReader.ReadAsync();
            var bytes = body.Buffer.ToArray();
            var serializer = new BinarySerializer();
            var externalCall = serializer.ConvertToObject<ExternalCallArgs>(bytes);
            return await ExternalCallJson(externalCall);
        }

        [HttpPost(Constants.ExternalCallAction + "Json")]
        public Task<int> ExternalCallJson(ExternalCallArgs externalCall)
        {
            return ReceiveExternalCall(externalCall);
        }

        public async Task<int> ReceiveExternalCall(ExternalCallArgs externalCall)
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
                    },
                    Created = externalCall.Created,
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
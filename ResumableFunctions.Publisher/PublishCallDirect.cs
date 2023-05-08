using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Json;

namespace ResumableFunctions.Publisher
{
    public class PublishCallDirect : IPublishCall
    {
        private readonly IPublisherSettings _settings;
        private readonly HttpClient _client;
        private readonly ILogger<PublishCallDirect> _logger;

        public PublishCallDirect(IPublisherSettings settings, HttpClient client, ILogger<PublishCallDirect> logger)
        {
            _settings = settings;
            _client = client;
            _logger = logger;
        }

        public async Task Publish<TInput, TOutput>(Func<TInput, Task<TOutput>> methodToPush,
            TInput input,
            TOutput output,
            string methodUrn,
            string serviceName)
        {
            await Publish(new MethodCall
            {
                MethodUrn = methodUrn,
                Input = input,
                Output = output,
                ServiceName = serviceName
            });
        }

        public async Task Publish(MethodCall methodCall)
        {
            try
            {
                //call `/api/ResumableFunctions/ExternalCall`
                //_functionHandler.QueuePushedCallProcessing(_pushedCall).Wait();
                var serviceUrl = _settings.ServicesRegistry[methodCall.ServiceName];
                var actionUrl =
                    $"{serviceUrl}api/ResumableFunctions/ExternalCall";
                var resposne = await _client.PostAsJsonAsync(actionUrl, methodCall);
                resposne.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured when Publish method call {methodCall}");
            }

        }
    }
}

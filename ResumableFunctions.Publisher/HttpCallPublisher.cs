using MessagePack.Resolvers;
using MessagePack;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Publisher.InOuts;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;

namespace ResumableFunctions.Publisher
{
    public class HttpCallPublisher : ICallPublisher
    {
        private readonly IPublisherSettings _settings;
        private readonly HttpClient _client;
        private readonly ILogger<HttpCallPublisher> _logger;

        public HttpCallPublisher(IPublisherSettings settings, HttpClient client, ILogger<HttpCallPublisher> logger)
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
                MethodData = new MethodData { MethodUrn = methodUrn },
                Input = input,
                Output = output,
                ServiceName = serviceName
            });
        }

        public async Task Publish(MethodCall methodCall)
        {
            try
            {
                var serviceUrl = _settings.ServicesRegistry[methodCall.ServiceName];
                var actionUrl =
                    $"{serviceUrl}api/ResumableFunctions/ExternalCall";
                var body = MessagePackSerializer.Serialize(methodCall, ContractlessStandardResolver.Options);
                //create a System.Net.Http.MultiPartFormDataContent
                
                var response = await _client.PostAsync(actionUrl, new ByteArrayContent(body));
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                //result may be 1 or -1
                //todo:[publisher] queue failed requests to be processed later here and in the below exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured when Publish method call {methodCall}");
            }

        }
    }
}

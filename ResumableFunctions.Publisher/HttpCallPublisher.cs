using MessagePack.Resolvers;
using MessagePack;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Publisher.InOuts;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;
using LiteDB;

namespace ResumableFunctions.Publisher
{
    public class HttpCallPublisher : ICallPublisher
    {
        private readonly IPublisherSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpCallPublisher> _logger;
        private readonly IFailedRequestHandler _failedRequestHandler;

        public HttpCallPublisher(
            IPublisherSettings settings,
            IHttpClientFactory httpClientFactory,
            ILogger<HttpCallPublisher> logger,
            IFailedRequestHandler failedRequestHandler)
        {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _failedRequestHandler = failedRequestHandler;
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
            var failedRequest = new FailedRequest();
            try
            {
                var serviceUrl = _settings.ServicesRegistry[methodCall.ServiceName];
                var actionUrl =
                    $"{serviceUrl}{Constants.ResumableFunctionsControllerUrl}/{Constants.ExternalCallAction}";
                failedRequest.ActionUrl = actionUrl;

                var body = MessagePackSerializer.Serialize(methodCall, ContractlessStandardResolver.Options);
                failedRequest.Body = body;

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(actionUrl, new ByteArrayContent(body));
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                if (!(result == "1" || result == "-1"))
                    throw new Exception("Expected result must be 1 or -1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured when publish method call {methodCall}");
                _failedRequestHandler.AddFailedRequest(failedRequest);
            }

        }
    }
}

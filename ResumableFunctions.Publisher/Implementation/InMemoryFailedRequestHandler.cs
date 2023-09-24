using Microsoft.Extensions.Logging;
using ResumableFunctions.Publisher.Abstraction;
using ResumableFunctions.Publisher.InOuts;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
namespace ResumableFunctions.Publisher.Implementation
{
    internal class InMemoryFailedRequestHandler : IFailedRequestHandler
    {
        private readonly IPublisherSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InMemoryFailedRequestHandler> _logger;
        private static readonly ConcurrentBag<FailedRequest> _failedRequests = new ConcurrentBag<FailedRequest>();
        public InMemoryFailedRequestHandler(
           IPublisherSettings settings,
           IHttpClientFactory httpClientFactory,
           ILogger<InMemoryFailedRequestHandler> logger)
        {
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        public void AddFailedRequest(FailedRequest failedRequest)
        {
            try
            {
                failedRequest.Created = DateTime.Now;
                _failedRequests.Add(failedRequest);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Can't save failed request to db.");
            }
        }

        public async Task HandleFailedRequests()
        {
            while (true)
            {
                if (_failedRequests.Count == 0)
                    await Task.Delay(_settings.CheckFailedRequestEvery);
                if (_failedRequests.Count > 0)
                    CallFailedRequests();
            }
        }

        // ReSharper disable once FunctionRecursiveOnAllPaths
        private void CallFailedRequests()
        {
            try
            {
                _logger.LogInformation("Start handling failed requests.");
                _logger.LogInformation($"Found [{_failedRequests.Count}] failed request.");
                for (var i = 0; i < _failedRequests.Count; i++)
                {
                    _ = CallRequest();
                }
                _logger.LogInformation("End handling failed requests.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when handling failed requests.");
            }
        }

        private async Task CallRequest()
        {
            FailedRequest request = null;
            try
            {
                if (_failedRequests.TryTake(out request))
                {
                    var client = _httpClientFactory.CreateClient();
                    var response =
                        await client.PostAsync(request.ActionUrl, new ByteArrayContent(request.Body));
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadAsStringAsync();
                    if (!(result == "1" || result == "-1"))
                        throw new Exception("Expected result must be 1 or -1");
                }
            }
            catch (Exception)
            {
                if (request != null)
                {
                    request.AttemptsCount++;
                    _logger.LogError(
                        $"A request [{request.ActionUrl}] failed again for [{request.AttemptsCount}] times");
                    request.LastAttemptDate = DateTime.Now;
                    _failedRequests.Add(request);
                }
            }
        }
    }
}

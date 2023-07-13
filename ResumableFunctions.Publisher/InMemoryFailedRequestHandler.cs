using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ResumableFunctions.Publisher
{
    internal class InMemoryFailedRequestHandler : IFailedRequestHandler
    {
        private readonly IPublisherSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InMemoryFailedRequestHandler> _logger;
        private ConcurrentBag<FailedRequest> _failedRequests = new();
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
                failedRequest.Id = Guid.NewGuid();
                _failedRequests.Add(failedRequest);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Can't save failed request to db.");
            }
        }

        public void HandleFailedRequestes()
        {
            _ = CallFailedRequests();
            async Task CallFailedRequests()
            {
                try
                {
                    _logger.LogInformation("Start handling failed requests.");
                    _logger.LogInformation($"Found `{_failedRequests.Count()}` failed request.");
                    for (int i = 0; i < _failedRequests.Count; i++)
                    {

                        FailedRequest request = null;
                        try
                        {
                            if (_failedRequests.TryTake(out request))
                            {
                                var client = _httpClientFactory.CreateClient();
                                var response = await client.PostAsync(
                                    request.ActionUrl,
                                    new ByteArrayContent(request.Body));
                                response.EnsureSuccessStatusCode();
                                var result = await response.Content.ReadAsStringAsync();
                                if (!(result == "1" || result == "-1"))
                                    throw new Exception("Expected result must be 1 or -1");
                            }
                        }
                        catch (Exception)
                        {
                            request.AttemptsCount++;
                            _logger.LogInformation(
                                $"Request `{request.Id}` failed againg for `{request.AttemptsCount}` times");
                            request.LastAttemptDate = DateTime.Now;
                            _failedRequests.Add(request);
                            throw;
                        }
                    }
                    _logger.LogInformation("End handling failed requests.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when handling failed requests.");
                }
                finally
                {
                    await Task.Delay(_settings.CheckFailedRequestEvery);
                    await CallFailedRequests();
                }
            }
        }
    }
}

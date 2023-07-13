//using LiteDB;
//using Microsoft.Extensions.Logging;

//namespace ResumableFunctions.Publisher
//{
//    internal class LiteDbFailedRequestHandler : IFailedRequestHandler
//    {
//        private readonly IPublisherSettings _settings;
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly ILogger<LiteDbFailedRequestHandler> _logger;

//        public LiteDbFailedRequestHandler(
//            IPublisherSettings settings,
//            IHttpClientFactory httpClientFactory,
//            ILogger<LiteDbFailedRequestHandler> logger)
//        {
//            _settings = settings;
//            _httpClientFactory = httpClientFactory;
//            _logger = logger;
//        }

//        public void AddFailedRequest(FailedRequest failedRequest)
//        {
//            try
//            {
//                using (var db = new LiteDatabase(Constants.FailedRequestsDb))
//                {
//                    var failedRequests = db.GetCollection<FailedRequest>(Constants.FailedRequestsCollection);
//                    failedRequest.Created = DateTime.Now;
//                    failedRequests.Insert(failedRequest);
//                }
//            }
//            catch (Exception ex)
//            {

//                _logger.LogError(ex, "Can't save failed request to db.");
//            }
//        }

//        public void HandleFailedRequestes()
//        {
//            _ = CallFailedRequests();
//            async Task CallFailedRequests()
//            {
//                try
//                {
//                    _logger.LogInformation("Start handling failed requests.");
//                    using (var db = new LiteDatabase(Constants.FailedRequestsDb))
//                    {
//                        var failedRequests = db.GetCollection<FailedRequest>(Constants.FailedRequestsCollection);
//                        var requets = failedRequests.FindAll().ToList();
//                        for (int i = 0; i < requets.Count; i++)
//                        {
//                            FailedRequest request = requets[i];
//                            _logger.LogInformation($"Found `{failedRequests.Count()}` failed request.");
//                            try
//                            {
//                                var client = _httpClientFactory.CreateClient();
//                                var response = await client.PostAsync(
//                                    request.ActionUrl,
//                                    new ByteArrayContent(request.Body));
//                                response.EnsureSuccessStatusCode();
//                                var result = await response.Content.ReadAsStringAsync();
//                                if (!(result == "1" || result == "-1"))
//                                    throw new Exception("Expected result must be 1 or -1");
//                                failedRequests.Delete(request.Id);
//                            }
//                            catch (Exception)
//                            {
//                                request.AttemptsCount++;
//                                _logger.LogInformation(
//                                    $"Request `{request.Id}` failed againg for `{request.AttemptsCount}` times");
//                                request.LastAttemptDate = DateTime.Now;
//                                failedRequests.Update(request);
//                                throw;
//                            }
//                        }
//                    }
//                    _logger.LogInformation("End handling failed requests.");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error when handling failed requests.");
//                }
//                finally
//                {
//                    await Task.Delay(_settings.CheckFailedRequestEvery);
//                    await CallFailedRequests();
//                }
//            }
//        }
//    }
//}

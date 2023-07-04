using System.ComponentModel;
using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Helpers
{
    public class HangfireHttpClient
    {
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public HangfireHttpClient(IBackgroundProcess backgroundJobClient, IHttpClientFactory httpClientFactory)
        {
            _backgroundJobClient = backgroundJobClient;
            _httpClientFactory = httpClientFactory;
        }
        public async Task EnqueueGetRequestIfFail(string url)
        {
            try
            {
                await HttpGet(url);
            }
            catch (Exception)
            {
                _backgroundJobClient.Schedule(() => HttpGet(url), TimeSpan.FromSeconds(3));
            }

        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task HttpGet(string url)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
}

using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
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

        public async Task EnqueuePostRequestIfFail(string url, object payload)
        {
            try
            {
                await HttpPost(url, payload);
            }
            catch (Exception)
            {
                _backgroundJobClient.Schedule(() => HttpPost(url, payload), TimeSpan.FromSeconds(3));
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DisplayName("HTTP GET `{0}`")]
        public async Task HttpGet(string url)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DisplayName("HTTP POST `{0}`")]
        public async Task HttpPost(string url, object payload)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }
    }
}

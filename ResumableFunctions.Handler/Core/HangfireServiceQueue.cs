using System;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core
{
    internal class HangfireServiceQueue : IServiceQueue
    {
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public HangfireServiceQueue(IBackgroundProcess backgroundJobClient, IHttpClientFactory httpClientFactory)
        {
            _backgroundJobClient = backgroundJobClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task EnqueueCallImpaction(CallServiceImapction callImapction)
        {
            var actionUrl = $"{callImapction.ServiceUrl}{Constants.ResumableFunctionsControllerUrl}/{Constants.ServiceProcessPushedCallAction}";
            try
            {
                await HttpPost(actionUrl, callImapction);
            }
            catch (Exception)
            {
                _backgroundJobClient.Schedule(() => HttpPost(actionUrl, callImapction), TimeSpan.FromSeconds(3));
            }
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
        [DisplayName("HTTP POST `{0}`")]
        public async Task HttpPost(string url, object payload)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            if (!(result == "1" || result == "-1"))
                throw new Exception("Expected result must be 1 or -1");
        }
    }
}

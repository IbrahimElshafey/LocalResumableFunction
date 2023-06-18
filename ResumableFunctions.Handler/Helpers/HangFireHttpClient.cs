using System.ComponentModel;
using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Helpers
{
    public class HangfireHttpClient
    {
        private readonly IBackgroundProcess backgroundJobClient;
        private readonly HttpClient client;

        public HangfireHttpClient(IBackgroundProcess backgroundJobClient, HttpClient client)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.client = client;
        }
        public async Task EnqueueGetRequestIfFail(string url)
        {
            try
            {
                await HttpGet(url);
            }
            catch (Exception)
            {
                backgroundJobClient.Enqueue(() => HttpGet(url));
            }
            
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task HttpGet(string url)
        {
            var resposne = await client.GetAsync(url);
            resposne.EnsureSuccessStatusCode();
        }
    }
}

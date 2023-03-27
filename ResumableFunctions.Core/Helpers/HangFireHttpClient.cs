using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Core.Helpers
{
    public class HangFireHttpClient
    {
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly HttpClient client;

        public HangFireHttpClient(IBackgroundJobClient backgroundJobClient, HttpClient client)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.client = client;
        }
        public void EnqueueGetRequest(string url)
        {
            backgroundJobClient.Enqueue(() => HttpGet(url));
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public async Task HttpGet(string url)
        {
            await client.GetAsync(url);
        }
    }
}

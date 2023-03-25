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

        public HangFireHttpClient(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }
        public void EnqueueGetRequest(string url)
        {
            backgroundJobClient.Enqueue(() => Get(url));
        }

        public void Get(string url)
        {
            throw new NotImplementedException();
        }
    }
}

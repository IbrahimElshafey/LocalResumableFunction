using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core
{
    internal class CallPusher : ICallPusher
    {
        private readonly FunctionDataContext _context;
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly ICallProcessor _processor;
        private readonly ILogger<CallPusher> _logger;

        public CallPusher(
            FunctionDataContext context,
            IBackgroundProcess backgroundJobClient,
            ICallProcessor processor, ILogger<CallPusher> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _processor = processor;
            _logger = logger;
        }

        public async Task<int> PushCall(PushedCall pushedCall)
        {
            _context.PushedCalls.Add(pushedCall);
            await _context.SaveChangesAsync();
            _backgroundJobClient.Enqueue(() => _processor.InitialProcessPushedCall(pushedCall.Id, pushedCall.MethodData.MethodUrn));
            return pushedCall.Id;
        }

        public async Task<int> PushExternalCall(PushedCall pushedCall, string serviceName)
        {
            string methodUrn = pushedCall.MethodData.MethodUrn;
            if (await IsExternal(methodUrn) is false)
            {
                string errorMsg =
                    $"There is no method with URN [{methodUrn}] that can be called from external in service [{serviceName}].";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            return await PushCall(pushedCall);
        }

        internal async Task<bool> IsExternal(string methodUrn)
        {
            return await _context
                .MethodsGroups
                .Include(x => x.WaitMethodIdentifiers)
                .Where(x => x.MethodGroupUrn == methodUrn)
                .SelectMany(x => x.WaitMethodIdentifiers)
                .AnyAsync(x => x.CanPublishFromExternal);
        }
    }
}
using AspectInjector.Broker;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Attributes;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler.Helpers
{
    public class BackgroundJobExecutor
    {
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobExecutor> _logger;

        public BackgroundJobExecutor(
            IServiceProvider serviceProvider,
            IDistributedLockProvider lockProvider,
            ILogger<BackgroundJobExecutor> logger)
        {
            _serviceProvider = serviceProvider;
            _lockProvider = lockProvider;
            _logger = logger;
        }
        public async Task Execute(
            string lockName,
            Func<Task> backgroundTask,
            string errorMessage = null,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                using (var handle = await _lockProvider.TryAcquireLockAsync(lockName))
                {
                    if (handle is null) return;

                    using IServiceScope scope = _serviceProvider.CreateScope();
                    await backgroundTask();
                }
            }
            catch (Exception ex)
            {
                var codeInfo = $"Source File Path: {sourceFilePath}\n" +
                        $"Line Number: {sourceLineNumber}\n";
                if (errorMessage == null)
                    _logger.LogError(ex,
                        $"Error when execute `{methodName}`\n{codeInfo}");
                else
                    _logger.LogError(ex, errorMessage + codeInfo);
            }
        }
    }
}
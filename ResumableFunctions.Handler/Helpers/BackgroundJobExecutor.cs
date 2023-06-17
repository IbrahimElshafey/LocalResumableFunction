using System.Runtime.CompilerServices;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Helpers
{
    public class BackgroundJobExecutor
    {
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobExecutor> _logger;
        private readonly IResumableFunctionsSettings _settings;

        public BackgroundJobExecutor(
            IServiceProvider serviceProvider,
            IDistributedLockProvider lockProvider,
            ILogger<BackgroundJobExecutor> logger,
            IResumableFunctionsSettings settings)
        {
            _serviceProvider = serviceProvider;
            _lockProvider = lockProvider;
            _logger = logger;
            _settings = settings;
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
                await using var handle = await _lockProvider.TryAcquireLockAsync(_settings.CurrentDbName + lockName);
                if (handle is null) return;

                using IServiceScope scope = _serviceProvider.CreateScope();
                await backgroundTask();
            }
            catch (Exception ex)
            {
                var codeInfo = 
                    $"\nSource File Path: {sourceFilePath}\n" +
                    $"Line Number: {sourceLineNumber}";
                if (errorMessage == null)
                    _logger.LogError(ex,
                        $"Error when execute `{methodName}`\n{codeInfo}");
                else
                    _logger.LogError(ex, errorMessage + codeInfo);

                throw;
            }
        }
    }
}
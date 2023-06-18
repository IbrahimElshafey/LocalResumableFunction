using System.Runtime.CompilerServices;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Helpers
{
    public class BackgroundJobExecutor
    {
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobExecutor> _logger;
        private readonly IResumableFunctionsSettings _settings;
        private readonly IScanStateRepo _scanStateRepo;

        public BackgroundJobExecutor(
            IServiceProvider serviceProvider,
            IDistributedLockProvider lockProvider,
            ILogger<BackgroundJobExecutor> logger,
            IResumableFunctionsSettings settings,
            IScanStateRepo scanStateRepo)
        {
            _serviceProvider = serviceProvider;
            _lockProvider = lockProvider;
            _logger = logger;
            _settings = settings;
            _scanStateRepo = scanStateRepo;
        }
        public async Task Execute(
            string lockName,
            Func<Task> backgroundTask,
            string errorMessage,
            bool isScanTask = false,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                await using var handle = await _lockProvider.TryAcquireLockAsync(_settings.CurrentDbName + lockName);
                if (handle is null) return;//if another process work on same task then ignore

                using IServiceScope scope = _serviceProvider.CreateScope();
                var scanTaskId = 0;
                if (isScanTask)
                    scanTaskId = await _scanStateRepo.AddScanState(lockName);
                await backgroundTask();
                if (isScanTask)
                    await _scanStateRepo.RemoveScanState(scanTaskId);
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
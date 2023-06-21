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
            var hasException = false;
            var scanTaskId = 0;
            try
            {
                await using var handle = await _lockProvider.TryAcquireLockAsync(_settings.CurrentDbName + lockName);
                if (handle is null) return;//if another process work on same task then ignore

                using IServiceScope scope = _serviceProvider.CreateScope();
                if (isScanTask)
                    scanTaskId = await _scanStateRepo.AddScanState(lockName);
                await backgroundTask();
            }
            catch (Exception ex)
            {
                hasException = true;
                var codeInfo =
                    $"\nSource File Path: {sourceFilePath}\n" +
                    $"Line Number: {sourceLineNumber}";
                _logger.LogError(ex,
                        errorMessage == null ?
                        $"Error when execute `{methodName}`\n{codeInfo}" :
                        $"{errorMessage}\n{codeInfo}");
                throw;
            }
            finally
            {
                if (isScanTask && !hasException)
                    await _scanStateRepo.RemoveScanState(scanTaskId);
            }
        }
    }
}
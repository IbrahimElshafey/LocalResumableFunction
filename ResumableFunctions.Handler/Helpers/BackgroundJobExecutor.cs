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
        public static async Task Execute(
            string lockName,
            Func<Task> backgroundTask,
            string errorMessage = null,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var _lockProvider = WaitMethodAspect.ServiceProvider.GetService<IDistributedLockProvider>();
            var _logger = WaitMethodAspect.ServiceProvider.GetService<ILogger<BackgroundJobExecutor>>();
            try
            {
                using (var handle = await _lockProvider.TryAcquireLockAsync(lockName))
                {
                    if (handle is null) return;

                    using IServiceScope scope = WaitMethodAspect.ServiceProvider.CreateScope();
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
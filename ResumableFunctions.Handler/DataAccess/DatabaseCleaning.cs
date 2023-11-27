using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess
{
    internal class DatabaseCleaning : IDatabaseCleaning
    {
        private readonly WaitsDataContext _context;
        private readonly IServiceRepo _serviceRepo;
        private readonly IResumableFunctionsSettings _setting;

        public DatabaseCleaning(
            WaitsDataContext context,
            IServiceRepo serviceRepo,
            IResumableFunctionsSettings setting)
        {
            _context = context;
            _serviceRepo = serviceRepo;
            _setting = setting;
        }

        public async Task CleanCompletedFunctionInstances()
        {
            await AddLog("Start to delete compeleted functions instances.");
            var dateThreshold = DateTime.UtcNow.Subtract(_setting.CleanDbSettings.CompletedInstanceRetention);

            var instanceIds =
                await _context.FunctionStates
                .Where(instance => instance.Status == FunctionInstanceStatus.Completed && instance.Modified < dateThreshold)
                .Select(x => x.Id)
                .ToListAsync();
            if (instanceIds.Any())
            {
                var privateDataCount = _context.PrivateData
                 .Where(privateData => instanceIds.Contains(privateData.FunctionStateId))
                 .ExecuteDeleteAsync();

                var waitsCount = _context.Waits
                  .Where(wait => instanceIds.Contains(wait.FunctionStateId))
                  .ExecuteDeleteAsync();

                var instancesCount = _context.FunctionStates
                    .Where(functionState => instanceIds.Contains(functionState.Id))
                    .ExecuteDeleteAsync();

                var logsCount = _context.Logs
                    .Where(logItem => instanceIds.Contains((int)logItem.EntityId) && logItem.EntityType == nameof(ResumableFunctionState))
                    .ExecuteDeleteAsync();

                var waitProcessingCount = _context.WaitProcessingRecords
                    .Where(waitProcessingRecord => instanceIds.Contains(waitProcessingRecord.StateId))
                    .ExecuteDeleteAsync();
                await Task.WhenAll(waitsCount, instancesCount, logsCount, waitProcessingCount);

                await _serviceRepo.AddLogs(
                    LogType.Info,
                    StatusCodes.DataCleaning,
                    $"Delete [{logsCount.Result}] logs related to completed functions instances done.",
                    $"Delete [{instancesCount.Result}] compeleted functions instances done.",
                    $"Delete [{waitsCount.Result}] waits related to completed functions instances done.",
                    $"Delete [{waitProcessingCount.Result}] wait processing record related to completed functions instances done.");
            }
            await AddLog("Delete compeleted functions instances completed.");
        }

        public async Task CleanOldPushedCalls()
        {
            await AddLog("Start to delete old pushed calls.");
            var dateThreshold = DateTime.UtcNow.Subtract(_setting.CleanDbSettings.PushedCallRetention);
            var count =
                await _context.PushedCalls
                .Where(instance => instance.Created < dateThreshold)
                .ExecuteDeleteAsync();
            await AddLog($"Delete [{count}] old pushed calls.");
        }

        public async Task CleanSoftDeletedRows()
        {
            await AddLog("Start to delete soft deleted rows.");

            var count = await _context.Waits
             .Where(instance => instance.IsDeleted)
             .IgnoreQueryFilters()
             .ExecuteDeleteAsync();
            await AddLog($"Delete [{count}] soft deleted waits done.");

            count = await _context.FunctionStates
            .Where(instance => instance.IsDeleted)
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
            await AddLog($"Delete [{count}] soft deleted function state done.");
        }

        public async Task MarkInactiveWaitTemplates()
        {
            await AddLog("Start to deactivate unused wait templates.");
            var activeWaitTemplate =
                _context.MethodWaits
                .Where(x => x.Status == WaitStatus.Waiting)
                .Select(x => x.TemplateId)
                .Distinct();
            var count = await _context.WaitTemplates
                .Where(waitTemplate => waitTemplate.IsActive == 1 && !activeWaitTemplate.Contains(waitTemplate.Id))
                .ExecuteUpdateAsync(template => template
                    .SetProperty(x => x.IsActive, -1)
                    .SetProperty(x => x.DeactivationDate, DateTime.UtcNow));
            await AddLog($"Deactivate [{count}] unused wait templates done.");
        }

        public async Task CleanInactiveWaitTemplates()
        {
            await AddLog("Start to delete deactivated wait templates.");
            var dateThreshold = DateTime.UtcNow.Subtract(_setting.CleanDbSettings.DeactivatedWaitTemplateRetention);
            var count = await _context.WaitTemplates
                .Where(template => template.IsActive == -1 && template.DeactivationDate < dateThreshold)
                .ExecuteDeleteAsync();
            await AddLog($"Delete [{count}] deactivated wait templates done.");
        }

        private async Task AddLog(string message)
        {
            await _serviceRepo.AddLog(message, LogType.Info, StatusCodes.DataCleaning);
        }

        private async Task AddError(string message, Exception ex = null)
        {
            await _serviceRepo.AddErrorLog(ex, message, StatusCodes.DataCleaning);
        }
    }
}

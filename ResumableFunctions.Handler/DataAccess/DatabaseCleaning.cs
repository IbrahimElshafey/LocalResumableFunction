using Hangfire;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

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
            RecurringJob.AddOrUpdate(",]", () => CleanCompletedFunctionInstances(), Cron.Daily);
        }

        public async Task CleanCompletedFunctionInstances()
        {
            await AddLog("Start to delete compeleted functions instances.");
            var dateThreshold = DateTime.Now.Subtract(_setting.CleanDbSettings.CompletedInstanceRetention);

            var instanceIds =
                await _context.FunctionStates
                .Where(instance => instance.Status == FunctionStatus.Completed && instance.Modified > dateThreshold)
                .Select(x => x.Id)
                .ToListAsync();
            if (instanceIds.Any())
            {
                await _context.Waits
              .Where(wait => instanceIds.Contains(wait.FunctionStateId))
              .ExecuteDeleteAsync();
                await _context.FunctionStates
                    .Where(functionState => instanceIds.Contains(functionState.Id))
                    .ExecuteDeleteAsync();
                await _context.Logs
                    .Where(logItem => instanceIds.Contains((int)logItem.EntityId) && logItem.EntityType == nameof(ResumableFunctionState))
                    .ExecuteDeleteAsync();
                await _context.WaitProcessingRecords
                    .Where(waitProcessingRecord => instanceIds.Contains(waitProcessingRecord.StateId))
                    .ExecuteDeleteAsync();

            }
            await AddLog("Delete compeleted functions instances completed.");
        }

        public async Task CleanOldPushedCalls()
        {
            await AddLog("Start to delete old pushed calls.");
            var dateThreshold = DateTime.Now.Subtract(_setting.CleanDbSettings.PushedCallRetention);
            await _context.PushedCalls
                .Where(instance => instance.Created > dateThreshold)
                .ExecuteDeleteAsync();
            await AddLog("Delete compeleted functions instances completed.");
        }

        public async Task CleanSoftDeletedRows()
        {
            await AddLog("Start to delete soft deleted rows.");

            await _context.Waits
             .Where(instance => instance.IsDeleted)
             .IgnoreQueryFilters()
             .ExecuteDeleteAsync();

            await _context.FunctionStates
            .Where(instance => instance.IsDeleted)
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

            await AddLog("Delete soft deleted rows completed.");
        }

        public async Task MarkInactiveWaitTemplates()
        {
            await AddLog("Start to deactivate unused wait templates.");
            var activeWaitTemplate =
                _context.MethodWaits
                .Where(x => x.Status == WaitStatus.Waiting)
                .Select(x => x.TemplateId)
                .Distinct();
            await _context.WaitTemplates
                .Where(waitTemplate => !activeWaitTemplate.Contains(waitTemplate.Id))
                .ExecuteUpdateAsync(template => template
                    .SetProperty(x => x.IsActive, -1)
                    .SetProperty(x => x.DeactivationDate, DateTime.Now));
            await AddLog("Deactivate unused wait templates completed.");
        }

        public async Task CleanInactiveWaitTemplates()
        {
            await AddLog("Start to delete deactivated wait templates.");
            var dateThreshold = DateTime.Now.Subtract(_setting.CleanDbSettings.DeactivatedWaitTemplateRetention);
            await _context.WaitTemplates
                .Where(template => template.IsActive == -1 && template.DeactivationDate > dateThreshold)
                .ExecuteDeleteAsync();
            await AddLog("Delete deactivated wait templates completed.");
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

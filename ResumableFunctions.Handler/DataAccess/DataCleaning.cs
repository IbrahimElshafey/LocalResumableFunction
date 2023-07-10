using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.Reflection.Emit;

namespace ResumableFunctions.Handler.DataAccess
{
    internal class DataCleaning : IDataCleaning
    {
        private readonly WaitsDataContext _context;
        private readonly IServiceRepo _serviceRepo;
        private readonly IResumableFunctionsSettings _setting;

        public DataCleaning(
            WaitsDataContext context,
            IServiceRepo serviceRepo,
            IResumableFunctionsSettings setting)
        {
            _context = context;
            _serviceRepo = serviceRepo;
            _setting = setting;
        }

        public async Task DeleteCompletedFunctionInstances()
        {
            await AddLog("Start to delete compeleted functions instances.");
            var dateThreshold = DateTime.Now.Subtract(_setting.CleanDbSettings.CompletedInstanceRetentionPeriod);

            var instanceIds =
                await _context.FunctionStates
                .Where(instance => instance.Status == FunctionStatus.Completed && instance.Modified > dateThreshold)
                .Select(x => x.Id)
                .ToListAsync();

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

            await AddLog("Delete compeleted functions instances completed.");
        }

        public async Task DeleteOldPushedCalls()
        {
            await AddLog("Start to delete old pushed calls.");
            var dateThreshold = DateTime.Now.Subtract(_setting.CleanDbSettings.PushedCallRetentionPeriod);
            await _context.PushedCalls
                .Where(instance => instance.Created > dateThreshold)
                .ExecuteDeleteAsync();
            await AddLog("Delete compeleted functions instances completed.");
        }

        public async Task DeleteSoftDeletedRows()
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

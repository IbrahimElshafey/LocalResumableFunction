using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core
{
    internal class WaitProcessor : IWaitProcessor
    {
        private readonly IFirstWaitProcessor _firstWaitProcessor;
        private readonly IRecycleBinService _recycleBinService;
        private readonly IReplayWaitProcessor _replayWaitProcessor;
        private readonly IWaitsService _waitsRepository;
        private IServiceProvider _serviceProvider;
        private MethodWait _methodWait;
        private PushedCall _pushedCall;
        private readonly ILogger<WaitProcessor> _logger;
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly FunctionDataContext _context;
        private readonly BackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDistributedLockProvider _lockProvider;
        //private MethodWait _methodWait;
        //private PushedCall _pushedCall;

        public WaitProcessor(
            IServiceProvider serviceProvider,
            ILogger<WaitProcessor> logger,
            IFirstWaitProcessor firstWaitProcessor,
            IRecycleBinService recycleBinService,
            IWaitsService waitsRepository,
            IBackgroundProcess backgroundJobClient,
            FunctionDataContext context,
            IReplayWaitProcessor replayWaitProcessor,
            BackgroundJobExecutor backgroundJobExecutor,
            IDistributedLockProvider lockProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _firstWaitProcessor = firstWaitProcessor;
            _recycleBinService = recycleBinService;
            _waitsRepository = waitsRepository;
            _backgroundJobClient = backgroundJobClient;
            _context = context;
            _replayWaitProcessor = replayWaitProcessor;
            _backgroundJobExecutor = backgroundJobExecutor;
            _lockProvider = lockProvider;
        }

        public async Task ProcessWait(int mehtodWaitId, int pushedCallId)
        {
            await _backgroundJobExecutor.Execute(
                $"ProcessWait_{mehtodWaitId}_{pushedCallId}",
                async () =>
                {
                    _methodWait = await LoadWait(mehtodWaitId);
                    _pushedCall = await LoadPushedCall(pushedCallId);
                    if (_methodWait != null && _pushedCall != null)
                    {
                        var isSuccess = await Pipeline(
                            SetData,
                            CheckIfMatch,
                            CloneIfFirst,
                            UpdateFunctionData,
                            ResumeExecution);

                        if (isSuccess)
                        {
                            //todo:this must be the one call here
                            await _context.SaveChangesAsync();
                        }
                    }
                },
                $"Error when process wait `{mehtodWaitId}` that may be a match for pushed call `{pushedCallId}`");
        }



        private Task<bool> SetData()
        {
            _pushedCall.LoadUnmappedProps(_methodWait.MethodToWait.MethodInfo);
            _methodWait.Input = _pushedCall.Data.Input;
            _methodWait.Output = _pushedCall.Data.Output;
            return Task.FromResult(true);
        }
        private async Task<bool> CheckIfMatch()
        {
            var pushedCallId = _pushedCall.Id;
            try
            {
                var isMatch = _methodWait.IsMatched();
                if (isMatch)
                {
                    _methodWait.FunctionState.AddLog(
                        $"Wait matched [{_methodWait.Name}] for [{_methodWait.RequestedByFunction}].", LogType.Info);
                    await UpdateWaitToMatched(pushedCallId, _methodWait.Id);
                }
                else
                {
                    await UpdateWaitToUnmatched(pushedCallId, _methodWait.Id);
                }
                return isMatch;
            }
            catch (Exception ex)
            {
                _methodWait.FunctionState.AddError(
                    $"Error occured when evaluate match for [{_methodWait.Name}] in [{_methodWait.RequestedByFunction}] when pushed call [{pushedCallId}].", ex);
                return false;
            }
        }

        private async Task<bool> CloneIfFirst()
        {
            var pushedCallId = _pushedCall.Id;
            if (_methodWait.IsFirst)
            {
                var waitCall = await _context
                    .WaitsForCalls
                    .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == _methodWait.Id);
                _methodWait = await _firstWaitProcessor.CloneFirstWait(_methodWait);
                waitCall.WaitId = _methodWait.Id;
                await _context.SaveChangesAsync();
            }
            return true;
        }


        private async Task<bool> UpdateFunctionData()
        {
            var pushedCallId = _pushedCall.Id;
            try
            {
                using (await _lockProvider.AcquireLockAsync($"UpdateFunctionState_{_methodWait.FunctionStateId}"))
                {
                    if (_methodWait.UpdateFunctionData())
                        await _context.SaveChangesAsync();
                    else
                        throw new Exception(
                            $"Can't update function state `{_methodWait.FunctionStateId}` after method wait `{_methodWait}` matched.");
                }
                _methodWait.CurrentFunction.InitializeDependencies(_serviceProvider);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _methodWait.FunctionState.AddError(
                        $"Concurrency Exception occured when process wait [{_methodWait.Name}]." +
                        $"\nProcessing this wait will be scheduled.",
                        ex);
                _backgroundJobClient.Schedule(() => ProcessWait(_methodWait.Id, pushedCallId), TimeSpan.FromSeconds(2.5));
                return false;
            }
            return true;
        }

        private async Task<bool> ResumeExecution()
        {
            Wait currentWait = _methodWait;
            do
            {
                var parent = await _waitsRepository.GetWaitParent(currentWait);
                switch (currentWait)
                {
                    case MethodWait methodWait:
                        currentWait.Status = currentWait.IsFirst ? currentWait.Status : WaitStatus.Completed;
                        await GoNext(parent, methodWait);
                        await _context.SaveChangesAsync();
                        break;

                    case WaitsGroup:
                    case FunctionWait:
                        if (currentWait.IsCompleted())
                        {
                            currentWait.FunctionState.AddLog($"Wait `{currentWait.Name}` is completed.");
                            currentWait.Status = WaitStatus.Completed;
                            await _waitsRepository.CancelSubWaits(currentWait.Id);
                            await GoNext(parent, currentWait);
                        }
                        else return true;
                        break;
                }

                currentWait = parent;

            } while (currentWait != null);
            return true;
        }

        private async Task GoNext(Wait parent, Wait currentWait)
        {
            switch (parent)
            {
                case null:
                case FunctionWait:
                    await ProceedToNextWait(currentWait);
                    break;
                case WaitsGroup:
                    parent.FunctionState.AddLog($"Wait group ({parent.Name}) to complete.");
                    break;
            }
        }

        private async Task ProceedToNextWait(Wait currentWait)
        {
            try
            {
                //todo:bug:may cause problem for go back after
                if (currentWait.ParentWait != null && currentWait.ParentWait.Status != WaitStatus.Waiting)
                {
                    string errorMsg = $"Can't proceed to next ,Parent wait [{currentWait.ParentWait.Name}] status is not (Waiting).";
                    _logger.LogWarning(errorMsg);
                    currentWait.FunctionState.AddError(errorMsg);
                    return;
                }

                currentWait.Status = WaitStatus.Completed;
                var nextWait = await currentWait.GetNextWait();
                if (nextWait == null)
                {
                    if (currentWait.ParentWaitId == null)
                        await FinalExit(currentWait);
                    return;
                }

                _logger.LogInformation($"Get next wait [{nextWait.Name}] after [{currentWait.Name}]");

                nextWait.ParentWaitId = currentWait.ParentWaitId;
                currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
                nextWait.FunctionState = currentWait.FunctionState;
                _context.Entry(nextWait.FunctionState).State = EntityState.Modified;
                nextWait.RequestedByFunctionId = currentWait.RequestedByFunctionId;

                await SaveTheNewWait(nextWait);

            }
            catch (Exception ex)
            {
                var errorMessage = $"Error when proceed to next wait after {currentWait}";
                _logger.LogError(ex, errorMessage);
                currentWait.FunctionState.AddError(errorMessage, ex);
            }
        }

        private async Task SaveTheNewWait(Wait nextWait)
        {
            if (nextWait is ReplayRequest replayRequest)
            {
                var replayResult = await _replayWaitProcessor.ReplayWait(replayRequest);
                if (replayResult.ProceedExecution && replayResult.Wait != null)
                    await ProceedToNextWait(replayResult.Wait);
            }
            else
                await _waitsRepository.SaveWaitRequestToDb(nextWait);//next wait after resume function
            await _context.SaveChangesAsync();
        }

        private async Task FinalExit(Wait currentWait)
        {
            _logger.LogInformation($"Final exit for function instance `{currentWait.FunctionStateId}`");
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
            currentWait.FunctionState.AddLog("Function instance completed.", LogType.Info);
            currentWait.FunctionState.Status = FunctionStatus.Completed;
            await _waitsRepository.CancelOpenedWaitsForState(currentWait.FunctionStateId);
            await _recycleBinService.RecycleFunction(currentWait.FunctionStateId);
        }

        private async Task<MethodWait> LoadWait(int waitId)
        {
            var methodWait = await _context
                    .MethodWaits
                    .Include(x => x.RequestedByFunction)
                    .Include(x => x.FunctionState)
                    .Where(x => x.Status == WaitStatus.Waiting)
                    .FirstOrDefaultAsync(x => x.Id == waitId);

            if (methodWait == null)
            {
                var error = $"No method wait exist with ID ({waitId}) and status ({WaitStatus.Waiting}).";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.MethodToWait = await
                _context
                .WaitMethodIdentifiers
                .FindAsync(methodWait.MethodToWaitId);

            if (methodWait.MethodToWait == null)
            {
                var error = $"No method exist that linked to wait `{waitId}`.";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.Template = await
               _context
               .MethodWaitTemplates
               .Select(MethodWaitTemplate.BasicMatchSelector)
               .FirstAsync(x => x.Id == methodWait.TemplateId);
            if (methodWait.Template == null)
            {
                var error = $"No wait template exist for wait `{waitId}`.";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.FunctionState.LoadUnmappedProps(methodWait.RequestedByFunction.InClassType);
            methodWait.LoadUnmappedProps();
            return methodWait;
        }

        private async Task<PushedCall> LoadPushedCall(int pushedCallId)
        {
            try
            {
                var pushedCall = await _context
                   .PushedCalls
                   .FirstOrDefaultAsync(x => x.Id == pushedCallId);
                if (pushedCall == null)
                {
                    var error = $"No pushed method exist with ID ({pushedCallId}).";
                    _logger.LogError(error);
                    throw new Exception(error);
                }
                return pushedCall;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when process pushed method [{pushedCallId}] and wait [{_methodWait.Id}].", ex);
            }
        }
        private async Task UpdateWaitToMatched(int pushedCallId, int waitId)
        {

            try
            {
                var waitCall = await _context.WaitsForCalls
                     .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == waitId);
                waitCall.Status = WaitForCallStatus.Matched;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to UpdateWaitToMatched(pushedCallId:{pushedCallId}, waitId:{waitId})");
            }
        }
        private async Task UpdateWaitToUnmatched(int pushedCallId, int waitId)
        {
            try
            {
                var waitCall = await _context.WaitsForCalls
                    .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == waitId);
                waitCall.Status = WaitForCallStatus.NotMatched;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to UpdateWaitToUnmatched(pushedCallId:{pushedCallId}, waitId:{waitId})");
            }
        }

        private async Task<bool> Pipeline(params Func<Task<bool>>[] actions)
        {
            foreach (var action in actions)
                if (!await action())
                    return false;
            return true;
        }
    }


}

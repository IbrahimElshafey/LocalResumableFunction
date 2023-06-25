using System.Runtime.CompilerServices;
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
    internal class ExpectedMatchesProcessor : IExpectedMatchesProcessor
    {
        private readonly IFirstWaitProcessor _firstWaitProcessor;
        private readonly IRecycleBinService _recycleBinService;
        private readonly IReplayWaitProcessor _replayWaitProcessor;
        private readonly IWaitsRepo _waitsRepo;
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<ExpectedMatchesProcessor> _logger;
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly FunctionDataContext _context;
        private readonly BackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDistributedLockProvider _lockProvider;

        private WaitForCall _waitCall;
        private MethodWait _methodWait;
        private PushedCall _pushedCall;
        private readonly IResumableFunctionsSettings _settings;

        public ExpectedMatchesProcessor(
            IServiceProvider serviceProvider,
            ILogger<ExpectedMatchesProcessor> logger,
            IFirstWaitProcessor firstWaitProcessor,
            IRecycleBinService recycleBinService,
            IWaitsRepo waitsRepo,
            IBackgroundProcess backgroundJobClient,
            FunctionDataContext context,
            IReplayWaitProcessor replayWaitProcessor,
            BackgroundJobExecutor backgroundJobExecutor,
            IDistributedLockProvider lockProvider,
            IResumableFunctionsSettings settings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _firstWaitProcessor = firstWaitProcessor;
            _recycleBinService = recycleBinService;
            _waitsRepo = waitsRepo;
            _backgroundJobClient = backgroundJobClient;
            _context = context;
            _replayWaitProcessor = replayWaitProcessor;
            _backgroundJobExecutor = backgroundJobExecutor;
            _lockProvider = lockProvider;
            _settings = settings;
        }

        public async Task ProcessFunctionExpectedMatches(int functionId, int pushedCallId)
        {
            await _backgroundJobExecutor.Execute(
                $"ProcessFunctionExpectedMatchedWaits_{functionId}_{pushedCallId}",
                async () =>
                {
                    var waitsForCall =
                        await _context
                        .WaitsForCalls
                        .Where(x =>
                        x.PushedCallId == pushedCallId &&
                        x.FunctionId == functionId &&
                        x.ServiceId == _settings.CurrentServiceId &&
                        (x.MatchStatus == MatchStatus.PartiallyMatched ||
                         x.InstanceUpdateStatus == InstanceUpdateStatus.UpdateFailed))
                        .ToListAsync();

                    _pushedCall = await LoadPushedCall(pushedCallId);

                    foreach (var expectedMatch in waitsForCall)
                    {
                        _waitCall = expectedMatch;
                        _methodWait = await LoadWait(expectedMatch.WaitId);


                        var isSuccess = await Pipeline(
                            SetInputOutput,
                            CheckIfMatch,
                            CloneIfFirst,
                            UpdateFunctionData,
                            ResumeExecution);

                        if (!isSuccess) continue;

                        waitsForCall.ForEach(x =>
                            x.MatchStatus = x.MatchStatus == MatchStatus.PartiallyMatched ? MatchStatus.DuplicationCanceled : x.MatchStatus);
                        await _context.SaveChangesAsync();
                        break;
                    }
                },
                $"Error when process wait `{_methodWait?.Id}` that may be a match for pushed call `{pushedCallId}` and function `{functionId}`");
        }

        private Task<bool> SetInputOutput()
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
                        $"Wait matched [{_methodWait.Name}] for [{_methodWait.RequestedByFunction}].");
                    await UpdateWaitRecord(x => x.MatchStatus = MatchStatus.Matched);
                }
                else
                    await UpdateWaitRecord(x => x.MatchStatus = MatchStatus.NotMatched);
                return isMatch;
            }
            catch (Exception ex)
            {
                var error =
                    $"Error occurred when evaluate match for [{_methodWait.Name}] in [{_methodWait.RequestedByFunction}] when pushed call [{pushedCallId}].";
                _methodWait.FunctionState.AddError(error, ex);
                throw new Exception(error, ex);
            }
        }

        private async Task<bool> CloneIfFirst()
        {
            if (_methodWait.IsFirst)
            {
                _methodWait = await _firstWaitProcessor.CloneFirstWait(_methodWait);
                _waitCall.WaitId = _methodWait.Id;
                await _context.SaveChangesAsync();
            }
            return true;
        }


        private async Task<bool> UpdateFunctionData()
        {
            var pushedCallId = _pushedCall.Id;
            _methodWait.MatchedByCallId = pushedCallId;
            try
            {
                await using (await _lockProvider.AcquireLockAsync($"UpdateFunctionState_{_methodWait.FunctionStateId}"))
                {
                    if (_methodWait.UpdateFunctionData())
                    {
                        _context.Entry(_methodWait.FunctionState).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        await UpdateWaitRecord(x => x.InstanceUpdateStatus = InstanceUpdateStatus.UpdateSuccessed);
                    }
                    else
                    {
                        await UpdateWaitRecord(x => x.InstanceUpdateStatus = InstanceUpdateStatus.UpdateFailed);
                        throw new Exception(
                            $"Can't update function state `{_methodWait.FunctionStateId}` after method wait `{_methodWait}` matched.");

                    }
                }

                _methodWait.CurrentFunction.InitializeDependencies(_serviceProvider);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _methodWait.FunctionState.AddError(
                    $"Concurrency Exception occurred when process wait [{_methodWait.Name}]." +
                    $"\nProcessing this wait will be scheduled.",
                    ex);
                _backgroundJobClient.Schedule(() =>
                        ProcessFunctionExpectedMatches(_methodWait.RequestedByFunctionId, pushedCallId),
                    TimeSpan.FromSeconds(10));
                return false;
            }

            return true;
        }

        private async Task<bool> ResumeExecution()
        {
            try
            {
                Wait currentWait = _methodWait;
                do
                {
                    var parent = await _waitsRepo.GetWaitParent(currentWait);
                    switch (currentWait)
                    {
                        case MethodWait methodWait:
                            currentWait.Status = WaitStatus.Completed;
                            await GoNext(parent, methodWait);
                            await _context.SaveChangesAsync();
                            break;

                        case WaitsGroup:
                        case FunctionWait:
                            if (currentWait.IsCompleted())
                            {
                                currentWait.FunctionState.AddLog($"Wait `{currentWait.Name}` is completed.");
                                currentWait.Status = WaitStatus.Completed;
                                await _waitsRepo.CancelSubWaits(currentWait.Id);
                                await GoNext(parent, currentWait);
                            }
                            else
                            {
                                await UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionSuccessed);
                                return true;
                            }
                            break;
                    }

                    currentWait = parent;

                } while (currentWait != null);

            }
            catch (Exception)
            {
                await UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionFailed);
                throw;
            }
            await UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionSuccessed);
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
                throw;
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
                await _waitsRepo.SaveWait(nextWait);//next wait after resume function
            await _context.SaveChangesAsync();
        }

        private async Task FinalExit(Wait currentWait)
        {
            _logger.LogInformation($"Final exit for function instance `{currentWait.FunctionStateId}`");
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
            currentWait.FunctionState.AddLog("Function instance completed.");
            currentWait.FunctionState.Status = FunctionStatus.Completed;
            await _waitsRepo.CancelOpenedWaitsForState(currentWait.FunctionStateId);
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
               .WaitTemplates
               .Select(WaitTemplate.BasicMatchSelector)
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
                   .FindAsync(pushedCallId);

                if (pushedCall != null) return pushedCall;

                var error = $"No pushed method exist with ID ({pushedCallId}).";
                _logger.LogError(error);
                throw new Exception(error);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when process pushed method [{pushedCallId}] and wait [{_methodWait.Id}].", ex);
            }
        }
        private async Task UpdateWaitRecord(Action<WaitForCall> action, [CallerMemberName] string calledBy = "")
        {
            try
            {
                action(_waitCall);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Failed to execute `UpdateWaitRecord when [pushedCallId:{_pushedCall.Id}, waitId:{_methodWait.Id}, CalledBy:{calledBy}]`");
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

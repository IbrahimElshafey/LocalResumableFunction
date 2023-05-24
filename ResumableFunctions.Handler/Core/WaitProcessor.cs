using Hangfire;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ResumableFunctions.Handler.Core
{
    internal class WaitProcessor : IWaitProcessor
    {
        private readonly IFirstWaitProcessor _firstWaitProcessor;
        private readonly IRecycleBinService _recycleBinService;
        private readonly IReplayWaitProcessor _replayWaitProcessor;
        private readonly IWaitsRepository _waitsRepository;
        private IServiceProvider _serviceProvider;
        private readonly ILogger<WaitProcessor> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly FunctionDataContext _context;
        private readonly BackgroundJobExecutor _backgroundJobExecutor;
        private MethodWait _methodWait;
        private int _pushedCallId;
        private PushedCall _pushedCall;

        public WaitProcessor(
            IServiceProvider serviceProvider,
            ILogger<WaitProcessor> logger,
            IFirstWaitProcessor firstWaitProcessor,
            IRecycleBinService recycleBinService,
            IWaitsRepository waitsRepository,
            IBackgroundJobClient backgroundJobClient,
            FunctionDataContext context,
            IReplayWaitProcessor replayWaitProcessor,
            BackgroundJobExecutor backgroundJobExecutor)
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
        }

        public async Task RequestProcessing(int mehtodWaitId, int pushedCallId)
        {
            await _backgroundJobExecutor.Execute(
                $"WaitProcessor_RequestProcessing_{mehtodWaitId}_{pushedCallId}",
                async () =>
                {
                    _pushedCallId = pushedCallId;
                    if (await LoadWaitAndPushedCall(mehtodWaitId, pushedCallId))
                        await Pipeline(
                            SetInputAndOutput,
                            CheckIfMatch,
                            CloneIfFirst,
                            UpdateFunctionData,
                            CreateFunctionInstance,
                            ResumeExecution);
                    await _context.SaveChangesAsync();
                },
                $"Error when process wait `{mehtodWaitId}` that may be a match for pushed call `{pushedCallId}`");
        }

        private Task<bool> SetInputAndOutput(MethodWait methodWait, int pushedCallId)
        {
            var setInputOutputResult = methodWait.SetInputAndOutput();
            if (setInputOutputResult.Result is false)
            {
                methodWait.FunctionState.AddError(
                    $"Error occured when deserialize `Input or Output` for wait `{methodWait.Name}`,Pushed call id was `{pushedCallId}`");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        private async Task<bool> CheckIfMatch(MethodWait methodWait, int pushedCallId)
        {
            try
            {
                methodWait.LoadExpressions();
                var isMatch = methodWait.IsMatched();
                if (isMatch)
                {
                    methodWait.FunctionState.AddLog(
                        $"Wait matched [{methodWait.Name}] for [{methodWait.RequestedByFunction}].", LogType.Info);
                    await UpdateWaitToMatched(pushedCallId, methodWait.Id);
                }
                else
                {
                    await UpdateWaitToNotMatched(pushedCallId, methodWait.Id);
                }
                return isMatch;
            }
            catch (Exception ex)
            {
                methodWait.FunctionState.AddError(
                    $"Error occured when evaluate match for [{methodWait.Name}] in [{methodWait.RequestedByFunction}] when pushed call [{pushedCallId}].", ex);
                return false;
            }
        }

        private async Task<bool> CloneIfFirst(MethodWait methodWait, int pushedCallId)
        {
            if (methodWait.IsFirst)
            {
                var waitCall = await _context
                    .WaitsForCalls
                    .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == methodWait.Id);
                _methodWait = await _firstWaitProcessor.CloneFirstWait(methodWait);
                waitCall.WaitId = _methodWait.Id;
                await _context.SaveChangesAsync();
            }
            return true;
        }


        private async Task<bool> UpdateFunctionData(MethodWait methodWait, int pushedCallId)
        {
            var result = methodWait.UpdateFunctionData();
            await _context.SaveChangesAsync();
            return result;
        }

        private async Task<bool> CreateFunctionInstance(MethodWait methodWait, int pushedCallId)
        {
            //todo: should I use scope 
            using var scope = _serviceProvider.CreateScope();
            var currentFunctionClassWithDi =
                scope.ServiceProvider.GetService(methodWait.CurrentFunction.GetType()) ??
                ActivatorUtilities.CreateInstance(_serviceProvider, methodWait.CurrentFunction.GetType());
            JsonConvert.PopulateObject(
                JsonConvert.SerializeObject(methodWait.CurrentFunction), currentFunctionClassWithDi);
            methodWait.CurrentFunction = (ResumableFunction)currentFunctionClassWithDi;
            methodWait.FunctionState.UserDefinedId =
                methodWait.CurrentFunction.GetInstanceId(methodWait.RequestedByFunction.RF_MethodUrn);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                methodWait.FunctionState.AddError(
                        $"Concurrency Exception occured when process wait [{methodWait.Name}]." +
                        $"\nProcessing this wait will be scheduled.",
                        ex);
                _backgroundJobClient.Schedule(() => RequestProcessing(methodWait.Id, pushedCallId), TimeSpan.FromMinutes(3));
                return false;
            }
            return true;
        }

        private async Task<bool> ResumeExecution(MethodWait matchedMethodWait, int pushedCallId)
        {
            Wait currentWait = matchedMethodWait;
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

        private async Task<bool> LoadWaitAndPushedCall(int waitId, int pushedCallId)
        {
            try
            {
                _methodWait = await _context
                    .MethodWaits
                    .Include(x => x.RequestedByFunction)
                    .Include(x => x.MethodToWait)
                    .Include(x => x.FunctionState)
                    .Where(x => x.Status == WaitStatus.Waiting)
                    .FirstOrDefaultAsync(x => x.Id == waitId);

                if (_methodWait == null)
                {
                    _logger.LogError($"No method wait exist with ID ({waitId}) and status ({WaitStatus.Waiting}).");
                    return false;
                }

                _pushedCall = await _context
                   .PushedCalls
                   .FirstOrDefaultAsync(x => x.Id == pushedCallId);
                if (_pushedCall == null)
                {
                    _logger.LogError($"No pushed method exist with ID ({pushedCallId}).");
                    return false;
                }

                var targetMethod = await _context
                       .WaitMethodIdentifiers
                       .AnyAsync(x => x.Id == _methodWait.MethodToWaitId);

                if (targetMethod == false)
                {
                    _logger.LogError($"No method like ({_pushedCall.MethodData}) exist that match for pushed call `{pushedCallId}`.");
                    return false;
                }

                _methodWait.Input = _pushedCall.Input;
                _methodWait.Output = _pushedCall.Output;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when process pushed method [{pushedCallId}] and wait [{waitId}].");
                return false;
            }
        }
        private async Task UpdateWaitToMatched(int pushedCallId, int waitId)
        {

            try
            {
                var waitCall = await _context.WaitsForCalls
                     .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == waitId);
                waitCall.Status = WaitForCallStatus.Matched;//todo:update concurrency may occure
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to increment completed counter for PushedCall:{pushedCallId} and Wait:{waitId}");
            }
        }
        private async Task UpdateWaitToNotMatched(int pushedCallId, int waitId)
        {
            try
            {
                var waitCall = await _context.WaitsForCalls
                    .FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == waitId);
                waitCall.Status = WaitForCallStatus.NotMatched;//todo:update concurrency may occure
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to increment NotMatch counter for PushedCall:{pushedCallId} and Wait:{waitId}");
            }
        }

        private async Task<bool> Pipeline(params Func<MethodWait, int, Task<bool>>[] actions)
        {
            foreach (var action in actions)
                if (!await action(_methodWait, _pushedCallId))
                    return false;
            return true;
        }
    }


}

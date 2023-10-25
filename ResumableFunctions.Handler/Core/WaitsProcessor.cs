﻿using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler.Core
{
    internal class WaitsProcessor : IWaitsProcessor
    {
        private readonly IFirstWaitProcessor _firstWaitProcessor;
        private readonly IReplayWaitProcessor _replayWaitProcessor;
        private readonly IWaitsRepo _waitsRepo;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WaitsProcessor> _logger;
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly IUnitOfWork _context;
        private readonly BackgroundJobExecutor _backgroundJobExecutor;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWaitProcessingRecordsRepo _waitProcessingRecordsRepo;
        private readonly IMethodIdsRepo _methodIdsRepo;
        private readonly IWaitTemplatesRepo _templatesRepo;
        private readonly IPushedCallsRepo _pushedCallsRepo;
        private readonly IServiceRepo _serviceRepo;
        private readonly IResumableFunctionsSettings _settings;
        private WaitProcessingRecord _waitCall;
        private MethodWaitEntity _methodWait;
        private PushedCall _pushedCall;

        public WaitsProcessor(
            IServiceProvider serviceProvider,
            ILogger<WaitsProcessor> logger,
            IFirstWaitProcessor firstWaitProcessor,
            IWaitsRepo waitsRepo,
            IBackgroundProcess backgroundJobClient,
            IUnitOfWork context,
            IReplayWaitProcessor replayWaitProcessor,
            BackgroundJobExecutor backgroundJobExecutor,
            IDistributedLockProvider lockProvider,
            IWaitProcessingRecordsRepo waitsForCallsRepo,
            IMethodIdsRepo methodIdsRepo,
            IWaitTemplatesRepo templatesRepo,
            IPushedCallsRepo pushedCallsRepo,
            IServiceRepo serviceRepo,
            IResumableFunctionsSettings settings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _firstWaitProcessor = firstWaitProcessor;
            _waitsRepo = waitsRepo;
            _backgroundJobClient = backgroundJobClient;
            _context = context;
            _replayWaitProcessor = replayWaitProcessor;
            _backgroundJobExecutor = backgroundJobExecutor;
            _lockProvider = lockProvider;
            _waitProcessingRecordsRepo = waitsForCallsRepo;
            _methodIdsRepo = methodIdsRepo;
            _templatesRepo = templatesRepo;
            _pushedCallsRepo = pushedCallsRepo;
            _serviceRepo = serviceRepo;
            _settings = settings;
        }

        [DisplayName("Process Function Expected Matches where [FunctionId:{0}], [PushedCallId:{1}], [MethodGroupId:{2}]")]
        public async Task ProcessFunctionExpectedWaits(int functionId, long pushedCallId, int methodGroupId)
        {
            await _backgroundJobExecutor.Execute(
                $"ProcessFunctionExpectedMatchedWaits_{functionId}_{pushedCallId}",
                async () =>
                {
                    _pushedCall = await LoadPushedCall(pushedCallId);
                    var waitTemplates = await _templatesRepo.GetWaitTemplatesForFunction(methodGroupId, functionId);
                    var matchExist = false;
                    if (waitTemplates == null)
                        return;
                    foreach (var template in waitTemplates)
                    {
                        var waits = await _waitsRepo.GetWaitsForTemplate(
                            template,
                            _pushedCall.GetMandatoryPart(template.CallMandatoryPartExpression),
                            x => x.RequestedByFunction,
                            x => x.FunctionState);
                        if (waits == null)
                            continue;
                        foreach (var wait in waits)
                        {
                            await LoadWaitProps(wait);
                            wait.Template = template;
                            _waitCall =
                                 _waitProcessingRecordsRepo.Add(
                                    new WaitProcessingRecord
                                    {
                                        FunctionId = functionId,
                                        PushedCallId = pushedCallId,
                                        ServiceId = template.ServiceId,
                                        WaitId = wait.Id,
                                        StateId = wait.FunctionStateId,
                                        TemplateId = template.Id
                                    });

                            _methodWait = wait;

                            var isSuccess = await Pipeline(
                                SetInputOutput,
                                CheckIfMatch,
                                CloneIfFirst,
                                ExecuteAfterMatchAction,
                                ResumeExecution);

                            await _context.SaveChangesAsync();

                            if (!isSuccess) continue;

                            matchExist = true;
                            break;
                        }

                        if (matchExist) break;
                    }
                },
                $"Error when process wait [{_methodWait?.Id}] that may be a match for pushed call [{pushedCallId}] and function [{functionId}]");
        }

        private async Task LoadWaitProps(MethodWaitEntity methodWait)
        {

            methodWait.MethodToWait = await _methodIdsRepo.GetMethodIdentifierById(methodWait.MethodToWaitId);

            if (methodWait.MethodToWait == null)
            {
                var error = $"No method exist that linked to wait [{methodWait.MethodToWaitId}].";
                _logger.LogError(error);
                throw new Exception(error);
            }
            methodWait.FunctionState.LoadUnmappedProps(methodWait.RequestedByFunction.InClassType);
            methodWait.LoadUnmappedProps();
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
            _methodWait.CurrentFunction.InitializeDependencies(_serviceProvider);
            var pushedCallId = _pushedCall.Id;
            try
            {
                var isMatch = _methodWait.IsMatched();
                if (!isMatch)
                    UpdateWaitRecord(x => x.MatchStatus = MatchStatus.NotMatched);
                else
                {
                    var message =
                        $"Wait [{_methodWait.Name}] matched in [{_methodWait.RequestedByFunction.RF_MethodUrn}].";

                    if (_methodWait.IsFirst)
                        await _serviceRepo.AddLog(message, LogType.Info, StatusCodes.WaitProcessing);
                    else
                        _methodWait.FunctionState.AddLog(message, LogType.Info, StatusCodes.WaitProcessing);
                    UpdateWaitRecord(x => x.MatchStatus = MatchStatus.Matched);

                }
                return isMatch;
            }
            catch (Exception ex)
            {
                var error =
                    $"Error occurred when evaluate match for [{_methodWait.Name}] " +
                    $"in [{_methodWait.RequestedByFunction.RF_MethodUrn}] when pushed call [{pushedCallId}].";
                if (_methodWait.IsFirst)
                    await _serviceRepo.AddErrorLog(ex, error, StatusCodes.WaitProcessing);
                else
                    _methodWait.FunctionState.AddError(error, StatusCodes.WaitProcessing, ex);
                await _methodWait.CurrentFunction?.OnErrorOccurred(error, ex);
                throw new Exception(error, ex);
            }
        }

        private async Task<bool> CloneIfFirst()
        {
            if (_methodWait.IsFirst)
            {
                _methodWait = await _firstWaitProcessor.CloneFirstWait(_methodWait);
                _waitCall.WaitId = _methodWait.Id;
                _methodWait.FunctionState.Status = FunctionInstanceStatus.InProgress;
                await _context.SaveChangesAsync();
            }
            return true;
        }


        private async Task<bool> ExecuteAfterMatchAction()
        {
            //todo:[closure update] AfterMatchAction
            var pushedCallId = _pushedCall.Id;
            _methodWait.CallId = pushedCallId;
            try
            {
                await using (await _lockProvider.AcquireLockAsync($"{_settings.CurrentWaitsDbName}_UFS_{_methodWait.FunctionStateId}"))
                {
                    if (_methodWait.ExecuteAfterMatchAction())
                    {
                        _context.MarkEntityAsModified(_methodWait.FunctionState);
                        await _context.SaveChangesAsync();
                        UpdateWaitRecord(x => x.AfterMatchActionStatus = ExecutionStatus.ExecutionSucceeded);
                    }
                    else
                    {
                        _methodWait.Status = _settings.WaitStatusIfProcessingError;
                        UpdateWaitRecord(x => x.AfterMatchActionStatus = ExecutionStatus.ExecutionFailed);
                        throw new Exception(
                            $"Can't update function state [{_methodWait.FunctionStateId}] after method wait [{_methodWait}] matched.");
                    }
                }
                _methodWait.CurrentFunction.InitializeDependencies(_serviceProvider);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _methodWait.FunctionState.AddError(
                    $"Concurrency Exception occurred when process wait [{_methodWait.Name}]." +
                    $"\nProcessing this wait will be scheduled.",
                    StatusCodes.WaitProcessing, ex);

                _backgroundJobClient.Schedule(() =>
                        ProcessFunctionExpectedWaits(_methodWait.RequestedByFunctionId, pushedCallId, _methodWait.MethodGroupToWaitId),
                    TimeSpan.FromSeconds(10));
                return false;
            }
            catch (Exception ex)
            {
                await _methodWait.CurrentFunction?.OnErrorOccurred("Error when execute after match action.", ex);
            }

            return true;
        }

        private async Task<bool> ResumeExecution()
        {
            try
            {
                WaitEntity currentWait = _methodWait;
                do
                {
                    var parent = await _waitsRepo.GetWaitParent(currentWait);
                    switch (currentWait)
                    {
                        case MethodWaitEntity methodWait:
                            currentWait.Status = WaitStatus.Completed;
                            await GoNext(parent, methodWait);
                            await _context.SaveChangesAsync();
                            if (parent != null)
                                parent.CurrentFunction = methodWait.CurrentFunction;
                            break;

                        case WaitsGroupEntity:
                        case FunctionWaitEntity:
                            if (currentWait.IsCompleted())
                            {
                                currentWait.FunctionState.AddLog($"Wait [{currentWait.Name}] is completed.", LogType.Info, StatusCodes.WaitProcessing);
                                currentWait.Status = WaitStatus.Completed;
                                await _waitsRepo.CancelSubWaits(currentWait.Id, _pushedCall.Id);
                                //todo:[closure update] CancelSubWaits
                                await GoNext(parent, currentWait);
                            }
                            else
                            {
                                UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionSucceeded);
                                return true;
                            }
                            break;
                    }

                    currentWait = parent;

                } while (currentWait != null);

            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception occurred when try to resume execution after [{_methodWait.Name}].";
                _methodWait.FunctionState.AddError(errorMsg, StatusCodes.WaitProcessing, ex);
                _methodWait.FunctionState.Status = FunctionInstanceStatus.InError;
                _methodWait.Status = _settings.WaitStatusIfProcessingError;
                UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionFailed);
                await _methodWait.CurrentFunction?.OnErrorOccurred(errorMsg, ex);
                return false;
            }
            UpdateWaitRecord(x => x.ExecutionStatus = ExecutionStatus.ExecutionSucceeded);
            return true;
        }

        private async Task GoNext(WaitEntity parent, WaitEntity currentWait)
        {
            switch (parent)
            {
                case null:
                case FunctionWaitEntity:
                    await ProceedToNextWait(currentWait);
                    break;
                case WaitsGroupEntity:
                    parent.FunctionState.AddLog($"Wait group ({parent.Name}) to complete.", LogType.Info, StatusCodes.WaitProcessing);
                    currentWait.FunctionState.Status = FunctionInstanceStatus.InProgress;
                    break;
            }
        }

        private async Task ProceedToNextWait(WaitEntity currentWait)
        {
            try
            {
                if (currentWait.ParentWait != null && currentWait.ParentWait.Status != WaitStatus.Waiting)
                {
                    var errorMsg = $"Can't proceed to next ,Parent wait [{currentWait.ParentWait.Name}] status is not (Waiting).";
                    _logger.LogWarning(errorMsg);
                    currentWait.FunctionState.AddError(errorMsg, StatusCodes.WaitProcessing, null);
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
                nextWait.FunctionState.Status = FunctionInstanceStatus.InProgress;
                _context.MarkEntityAsModified(nextWait.FunctionState);
                await SaveTheNewWait(nextWait);

            }
            catch (Exception ex)
            {
                var errorMessage = $"Error when proceed to next wait after {currentWait}";
                _logger.LogError(ex, errorMessage);
                //currentWait.FunctionState.AddError(errorMessage, StatusCodes.WaitProcessing, ex);
                throw;
            }
        }

        private async Task SaveTheNewWait(WaitEntity nextWait)
        {
            if (nextWait is ReplayRequest replayRequest)
            {
                var replayResult = await _replayWaitProcessor.GetWaitToReplay(replayRequest);
                _context.MarkEntityAsModified(replayResult.Wait.FunctionState);
                if (replayResult is { ProceedExecution: true, Wait: not null })
                    await ProceedToNextWait(replayResult.Wait);
            }
            else
                await _waitsRepo.SaveWait(nextWait);
            await _context.SaveChangesAsync();
        }

        private async Task FinalExit(WaitEntity currentWait)
        {
            _logger.LogInformation($"Final exit for function instance [{currentWait.FunctionStateId}]");
            currentWait.Status = WaitStatus.Completed;
            currentWait.FunctionState.StateObject = currentWait.CurrentFunction;
            currentWait.FunctionState.AddLog("Function instance completed.", LogType.Info, StatusCodes.WaitProcessing);
            currentWait.FunctionState.Status = FunctionInstanceStatus.Completed;
            await _waitsRepo.CancelOpenedWaitsForState(currentWait.FunctionStateId);
        }

        private async Task<MethodWaitEntity> LoadWait(int waitId)
        {
            var methodWait = await _waitsRepo.GetMethodWait(waitId, x => x.RequestedByFunction, x => x.FunctionState);

            if (methodWait == null)
            {
                var error = $"No method wait exist with ID ({waitId}) and status ({WaitStatus.Waiting}).";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.MethodToWait = await _methodIdsRepo.GetMethodIdentifierById(methodWait.MethodToWaitId);

            if (methodWait.MethodToWait == null)
            {
                var error = $"No method exist that linked to wait [{waitId}].";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.Template = await _templatesRepo.GetWaitTemplateWithBasicMatch(methodWait.TemplateId);
            if (methodWait.Template == null)
            {
                var error = $"No wait template exist for wait [{waitId}].";
                _logger.LogError(error);
                throw new Exception(error);
            }

            methodWait.FunctionState.LoadUnmappedProps(methodWait.RequestedByFunction.InClassType);
            methodWait.LoadUnmappedProps();
            return methodWait;
        }

        private async Task<PushedCall> LoadPushedCall(long pushedCallId)
        {
            try
            {
                var pushedCall = await _pushedCallsRepo.GetById(pushedCallId);

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
        private void UpdateWaitRecord(Action<WaitProcessingRecord> action, [CallerMemberName] string calledBy = "")
        {
            try
            {
                action(_waitCall);
                //await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Failed to execute update wait record when [pushedCallId:{_pushedCall.Id}, waitId:{_methodWait.Id}, CalledBy:{calledBy}]");
            }
        }

        private async Task<bool> Pipeline(params Func<Task<bool>>[] actions)
        {
            await using (await _lockProvider.AcquireLockAsync($"{_settings.CurrentWaitsDbName}_WC_{_waitCall.Id}"))
            {
                foreach (var action in actions)
                    if (!await action())
                        return false;
                return true;
            }
        }
    }


}

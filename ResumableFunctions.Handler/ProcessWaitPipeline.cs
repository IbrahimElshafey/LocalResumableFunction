using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler
{
    internal class ProcessWaitPipeline
    {
        private readonly MethodWait methodWait;
        private readonly int pushedCallId;
        private FunctionDataContext _context;
        private readonly Func<MethodWait, Task<MethodWait>> cloneWaitIfFirstFunc;
        private readonly Func<MethodWait, Task> resumeExecutionFunc;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ProcessWaitPipeline(
            MethodWait mehtodWait,
            int pushedCallId,
            FunctionDataContext context,
            Func<MethodWait, Task<MethodWait>> cloneWaitIfFirstFunc,
            Func<MethodWait, Task> resumeExecutionFunc,
            IServiceProvider serviceProvider)
        {
            this.methodWait = mehtodWait;
            this.pushedCallId = pushedCallId;
            _context = context;
            this.cloneWaitIfFirstFunc = cloneWaitIfFirstFunc;
            this.resumeExecutionFunc = resumeExecutionFunc;
            _serviceProvider = serviceProvider;
            _backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        }

        public async Task Run()
        {
            await StopIfFalse(
                SetInputAndOutput,
                CheckIfMatch,
                CloneIfFirst,
                UpdateFunctionData,
                WakeUpInstanceAndResume);
            await _context.SaveChangesAsync();
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
                methodWait = await cloneWaitIfFirstFunc(methodWait);
                waitCall.WaitId = methodWait.Id;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        private Task<bool> UpdateFunctionData(MethodWait methodWait, int pushedCallId)
        {
            return Task.FromResult(methodWait.UpdateFunctionData());
        }

        private async Task<bool> WakeUpInstanceAndResume(MethodWait methodWait, int pushedCallId)
        {
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
                if (!methodWait.IsFirst)//save state changes if not first wait
                    await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                methodWait.FunctionState.AddError(
                        $"Concurrency Exception occured when process wait [{methodWait.Name}]." +
                        $"\nProcessing this wait will be scheduled.",
                        ex);
                _backgroundJobClient.Schedule(
                    () => ProcessExpectedWaitMatch(methodWait.Id, pushedCallId), TimeSpan.FromMinutes(3));
                return false;
            }

            await resumeExecutionFunc(methodWait);
            return true;
        }

        public async Task ProcessExpectedWaitMatch(int waitId, int pushedCallId)
        {
            try
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    SetDependencies(scope.ServiceProvider);

                    var methodWait = await _context
                        .MethodWaits
                        .Include(x => x.RequestedByFunction)
                        .Include(x => x.MethodToWait)
                        .Include(x => x.FunctionState)
                        .Where(x => x.Status == WaitStatus.Waiting)
                        .FirstOrDefaultAsync(x => x.Id == waitId);

                    if (methodWait == null)
                    {
                        _logger.LogError($"No method wait exist with ID ({waitId}) and status ({WaitStatus.Waiting}).");
                        return;
                    }

                    var pushedCall = await _context
                       .PushedCalls
                       .FirstOrDefaultAsync(x => x.Id == pushedCallId);
                    if (pushedCall == null)
                    {
                        _logger.LogError($"No pushed method exist with ID ({pushedCallId}).");
                        return;
                    }

                    var targetMethod = await _context
                           .WaitMethodIdentifiers
                           .FirstOrDefaultAsync(x => x.Id == methodWait.MethodToWaitId);

                    if (targetMethod == null)
                    {
                        _logger.LogError($"No method like ({pushedCall.MethodData}) exist that match for pushed call `{pushedCallId}`.");
                        return;
                    }

                    methodWait.Input = pushedCall.Input;
                    methodWait.Output = pushedCall.Output;

                    await ProcessWait(methodWait, pushedCall);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when process pushed method [{pushedCallId}] and wait [{waitId}].");
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
        private async Task<bool> StopIfFalse(params Func<MethodWait, int, Task<bool>>[] actions)
        {
            foreach (var action in actions)
                if (!await action(methodWait, pushedCallId))
                    return false;
            return true;
        }
    }


}

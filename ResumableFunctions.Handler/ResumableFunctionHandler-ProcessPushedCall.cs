using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.Helpers;
using Newtonsoft.Json.Linq;
using static System.Formats.Asn1.AsnWriter;
using Newtonsoft.Json;

namespace ResumableFunctions.Handler;

public partial class ResumableFunctionHandler
{
    internal FunctionDataContext _context;

    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResumableFunctionHandler> _logger;

    public ResumableFunctionHandler(IServiceProvider serviceProvider, ILogger<ResumableFunctionHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
    }

    internal async Task QueuePushedCallProcessing(PushedCall pushedCall)
    {
        SetDependencies(_serviceProvider);
        _context.PushedCalls.Add(pushedCall);
        await _context.SaveChangesAsync();
        _backgroundJobClient.Enqueue(() => ProcessPushedCall(pushedCall.Id));
    }

    public async Task ProcessPushedCall(int pushedCallId)
    {
        try
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                SetDependencies(scope.ServiceProvider);
                Debugger.Launch();
                var waitsIds = await _context.waitsRepository.GetWaitsIdsForMethodCall(pushedCallId);

                if (waitsIds != null)
                    foreach (var waitId in waitsIds)
                    {
                        if (IsLocalWait(waitId))
                            _backgroundJobClient.Enqueue(() => ProcessExpectedWaitMatch(waitId.Id, pushedCallId));
                        else
                            await CallOwnerService(waitId, pushedCallId);
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedCallId}]");
        }
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

    private async Task CallOwnerService(WaitId wait, int pushedCallId)
    {
        try
        {
            var ownerAssemblyName = wait.RequestedByAssembly;
            var ownerServiceUrl =
                await _context
                .ServicesData
                .Where(x => x.AssemblyName == ownerAssemblyName)
                .Select(x => x.Url)
                .FirstOrDefaultAsync();

            var actionUrl =
                $"{ownerServiceUrl}api/ResumableFunctions/ProcessMatchedWait?waitId={wait.Id}&pushedCallId={pushedCallId}";
            var hangFireHttpClient = _serviceProvider.GetService<HangFireHttpClient>();
            await hangFireHttpClient.EnqueueGetRequestIfFail(actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to call owner service for wait ({wait}).");
        }
    }

    private bool IsLocalWait(WaitId methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByAssembly;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
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
                await IncrementMatchedCounter(pushedCallId, methodWait.Id);
            }
            else
            {
                await IncrementNotMatchCounter(pushedCallId, methodWait.Id);
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

    private async Task IncrementNotMatchCounter(int pushedCallId, int waitId)
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

    private async Task ProcessWait(MethodWait methodWait, PushedCall pushedCall)
    {
        try
        {
            var pushedCallId = pushedCall.Id;
            if (methodWait.IsFirst)
            {
                var waitCall = await _context.WaitsForCalls.FirstAsync(x => x.PushedCallId == pushedCallId && x.WaitId == methodWait.Id);
                methodWait = await CloneFirstWait(methodWait);
                waitCall.WaitId = methodWait.Id;
                await _context.SaveChangesAsync();
            }

            //todo:log message `Wait is expected match`
            methodWait.FunctionState.AddLog(
                   $"Wait `{methodWait.Name}` may be a match for pushed call `{pushedCallId}`");

            var setInputOutputResult = methodWait.SetInputAndOutput();
            if (setInputOutputResult.Result is false)
            {
                methodWait.FunctionState.AddError(
                    $"Error occured when deserialize `Input or Output` for wait `{methodWait.Name}`,Pushed call id was `{pushedCallId}`");
                return;
            }

            if (await CheckIfMatch(methodWait, pushedCallId) is false)
                return;

            //if (methodWait.IsFirst)
            //    methodWait.FunctionState.StateObject = new JObject();

            if (methodWait.UpdateFunctionData())
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
                    return;
                }

                await ResumeExecution(methodWait);
                //await IncrementMatchedCounter(pushedCallId);
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Error when process matched wait for method [{methodWait.Name}] with id[{methodWait.Id}]");
        }

    }

    private async Task IncrementMatchedCounter(int pushedCallId, int waitId)
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

    internal void SetDependencies(IServiceProvider serviceProvider)
    {
        _context = serviceProvider.GetService<FunctionDataContext>();
    }
}
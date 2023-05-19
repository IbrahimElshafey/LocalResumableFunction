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

internal class PushedCallProcessor : IPushedCallProcessor
{
    internal FunctionDataContext _context;

    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitProcessor waitProcessor;

    public PushedCallProcessor(
        IServiceProvider serviceProvider,
        ILogger<ReplayWaitProcessor> logger,
        IWaitProcessor waitProcessor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        this.waitProcessor = waitProcessor;
        _backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
    }

    public async Task<int> QueuePushedCallProcessing(PushedCall pushedCall)
    {
        SetDependencies(_serviceProvider);
        _context.PushedCalls.Add(pushedCall);
        await _context.SaveChangesAsync();
        _backgroundJobClient.Enqueue(() => ProcessPushedCall(pushedCall.Id));
        return pushedCall.Id;
    }

    public async Task ProcessPushedCall(int pushedCallId)
    {
        try
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                SetDependencies(scope.ServiceProvider);
                var waitsIds = await _context.waitsRepository.GetWaitsIdsForMethodCall(pushedCallId);

                if (waitsIds != null)
                    foreach (var waitId in waitsIds)
                    {
                        if (IsLocalWait(waitId))
                            _backgroundJobClient.Enqueue(() => waitProcessor.Run(waitId.Id, pushedCallId));
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

    internal void SetDependencies(IServiceProvider serviceProvider)
    {
        _context = serviceProvider.GetService<FunctionDataContext>();
    }
}
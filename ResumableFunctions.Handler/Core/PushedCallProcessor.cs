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
using ResumableFunctions.Handler.Helpers;
using Newtonsoft.Json.Linq;
using static System.Formats.Asn1.AsnWriter;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.DataAccess;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Medallion.Threading;

namespace ResumableFunctions.Handler.Core;

internal class PushedCallProcessor : IPushedCallProcessor
{
    private readonly FunctionDataContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitProcessor _waitProcessor;
    private readonly IWaitsRepository _waitsRepository;
    private readonly HangFireHttpClient _hangFireHttpClient;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;

    public PushedCallProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitProcessor waitProcessor,
        IWaitsRepository waitsRepository,
        FunctionDataContext context,
        IBackgroundJobClient backgroundJobClient,
        HangFireHttpClient hangFireHttpClient,
        BackgroundJobExecutor backgroundJobExecutor)
    {
        _logger = logger;
        _waitProcessor = waitProcessor;
        _waitsRepository = waitsRepository;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _hangFireHttpClient = hangFireHttpClient;
        _backgroundJobExecutor = backgroundJobExecutor;
    }

    public async Task<int> QueuePushedCallProcessing(PushedCall pushedCall)
    {
        _context.PushedCalls.Add(pushedCall);
        await _context.SaveChangesAsync();
        _backgroundJobClient.Enqueue(() => ProcessPushedCall(pushedCall.Id));
        return pushedCall.Id;
    }

    public async Task ProcessPushedCall(int pushedCallId)
    {
        await _backgroundJobExecutor.Execute(
            $"PushedCallProcessor_ProcessPushedCall_{pushedCallId}",
            async () =>
            {
                var waitsIds = await _waitsRepository.GetWaitsIdsForMethodCall(pushedCallId);

                if (waitsIds != null)
                    foreach (var waitId in waitsIds)
                    {
                        if (IsLocalWait(waitId))
                            _backgroundJobClient.Enqueue(() => _waitProcessor.RequestProcessing(waitId.Id, pushedCallId));
                        else
                            await CallOwnerService(waitId, pushedCallId);
                    }
            },
            $"Error when process pushed method [{pushedCallId}]");
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
            await _hangFireHttpClient.EnqueueGetRequestIfFail(actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to call owner service for wait ({wait}).");
        }
    }

    private bool IsLocalWait(WaitId waitId)
    {
        var ownerAssemblyName = waitId.RequestedByAssembly;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
    }


    public async Task<int> QueueExternalPushedCallProcessing(PushedCall pushedCall, string serviceName)
    {
        string methodUrn = pushedCall.MethodData.MethodUrn;
        if (await IsExternal(methodUrn) is false)
        {
            string errorMsg =
                $"There is no method with URN [{methodUrn}] that can be called from external in service [{serviceName}].";
            _logger.LogError(errorMsg);
            throw new Exception(errorMsg);
        }
        return await QueuePushedCallProcessing(pushedCall);
    }

    internal async Task<bool> IsExternal(string methodUrn)
    {
        return await _context
            .MethodsGroups
            .Include(x => x.WaitMethodIdentifiers)
            .Where(x => x.MethodGroupUrn == methodUrn)
            .SelectMany(x => x.WaitMethodIdentifiers)
            .AnyAsync(x => x.CanPublishFromExternal);
    }
}
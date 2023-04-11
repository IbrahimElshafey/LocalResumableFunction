using System.Diagnostics;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using ResumableFunctions.Handler.Attributes;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;
using System;

namespace ResumableFunctions.Handler;

public partial class ResumableFunctionHandler
{
    internal FunctionDataContext _context;
    private WaitsRepository _waitsRepository;
    private MethodIdentifierRepository _metodIdsRepo;

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
                var matchedWaits = await _waitsRepository.GetMethodWaits(pushedCallId);

                if (matchedWaits != null)
                    foreach (var methodWait in matchedWaits)
                    {
                        if (IsLocalWait(methodWait))
                            await ProcessWait(methodWait);
                        else
                            await CallOwnerService(methodWait, pushedCallId);
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedCallId}]");
        }
    }

    private bool IsLocalWait(MethodWait methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
    }

    private async Task CallOwnerService(MethodWait methodWait, int pushedCallId)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        var ownerServiceUrl =
            await _context
            .ServicesData
            .Where(x => x.AssemblyName == ownerAssemblyName)
            .Select(x => x.Url)
            .FirstOrDefaultAsync();

        var actionUrl =
            $"{ownerServiceUrl}api/ResumableFunctions/ProcessMatchedWait?waitId={methodWait.Id}&pushedCallId={pushedCallId}";
        var hangFireHttpClient = _serviceProvider.GetService<HangFireHttpClient>();
        hangFireHttpClient.EnqueueGetRequest(actionUrl);
    }

    public async Task ProcessMatchedWait(int waitId, int pushedCallId)
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
                    //.Include(x => x.WaitMethodGroup)
                    .Where(x => x.Status == WaitStatus.Waiting)
                    .FirstAsync(x => x.Id == waitId);
                if (methodWait == null)
                {
                    _logger.LogError($"No method wait exist with ID ({waitId}) and status ({WaitStatus.Waiting}).");
                    return;
                }

                var pushedCall = await _context
                   .PushedCalls
                   .FirstAsync(x => x.Id == pushedCallId);
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
                    _logger.LogError($"No external method exist that match ({pushedCall.MethodData}).");
                    return;
                }

                methodWait.Input = pushedCall.Input;
                methodWait.Output = pushedCall.Output;
                await ProcessWait(methodWait);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedCallId}] and wait [{waitId}].");
        }
    }



    internal void SetDependencies(IServiceProvider serviceProvider)
    {
        _context = serviceProvider.GetService<FunctionDataContext>();
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
    }

}
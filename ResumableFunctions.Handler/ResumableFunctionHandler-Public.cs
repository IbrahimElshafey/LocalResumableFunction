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

    internal async Task QueuePushedMethodProcessing(PushedMethod pushedMethod)
    {
        SetDependencies(_serviceProvider);
        _context.PushedMethodsCalls.Add(pushedMethod);
        await _context.SaveChangesAsync();
        _backgroundJobClient.Enqueue(() => ProcessPushedMethod(pushedMethod.Id));
    }

    public async Task ProcessPushedMethod(int pushedMethodId)
    {
        try
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                SetDependencies(scope.ServiceProvider);
                Debugger.Launch();
                var waitsIds = await _waitsRepository.GetWaitsIdsForMethodCall(pushedMethodId);

                if (waitsIds != null)
                    foreach (var waitId in waitsIds)
                    {
                        if (IsLocalWait(waitId))
                            _backgroundJobClient.Enqueue(() => ProcessMatchedWait(waitId.Id, pushedMethodId));
                        //await ProcessWait(matchedMethodWait, pushedMethodId);
                        else
                            await CallOwnerService(waitId, pushedMethodId);
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedMethodId}]");
        }
    }

    private bool IsLocalWait(WaitId methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByAssembly;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
    }

    private async Task CallOwnerService(WaitId methodWait, int pushedMethodId)
    {
        var ownerAssemblyName = methodWait.RequestedByAssembly;
        var ownerServiceUrl =
            await _context
            .ServicesData
            .Where(x => x.AssemblyName == ownerAssemblyName)
            .Select(x => x.Url)
            .FirstOrDefaultAsync();

        var actionUrl =
            $"{ownerServiceUrl}api/ResumableFunctions/ProcessMatchedWait?waitId={methodWait.Id}&pushedMethodId={pushedMethodId}";
        var hangFireHttpClient = _serviceProvider.GetService<HangFireHttpClient>();
        await hangFireHttpClient.EnqueueGetRequestIfFail(actionUrl);
    }

    public async Task ProcessMatchedWait(int waitId, int pushedMethodId)
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

                var pushedMethod = await _context
                   .PushedMethodsCalls
                   .FirstAsync(x => x.Id == pushedMethodId);
                if (pushedMethod == null)
                {
                    _logger.LogError($"No pushed method exist with ID ({pushedMethodId}).");
                    return;
                }

                var targetMethod = await _context
                       .WaitMethodIdentifiers
                       .FirstOrDefaultAsync(x => x.Id == methodWait.MethodToWaitId);

                if (targetMethod == null)
                {
                    _logger.LogError($"No method exist that match ({pushedMethod.MethodData}).");
                    return;
                }

                methodWait.Input = pushedMethod.Input;
                methodWait.Output = pushedMethod.Output;
                await ProcessWait(methodWait, pushedMethodId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedMethodId}] and wait [{waitId}].");
        }
    }



    internal void SetDependencies(IServiceProvider serviceProvider)
    {
        _context = serviceProvider.GetService<FunctionDataContext>();
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
    }

}
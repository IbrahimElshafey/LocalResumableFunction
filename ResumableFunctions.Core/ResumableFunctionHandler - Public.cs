using System.Diagnostics;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using ResumableFunctions.Core.Attributes;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Core.Helpers;
using System;

namespace ResumableFunctions.Core;

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
                var matchedWaits = await _waitsRepository.GetMethodActiveWaits(pushedMethodId);

                if (matchedWaits != null)
                    foreach (var methodWait in matchedWaits)
                    {
                        if (IsLocalWait(methodWait))
                            await ProcessMatchedWait(methodWait);
                        else
                            await CallOwnerService(methodWait, pushedMethodId);
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when process pushed method [{pushedMethodId}]");
        }
    }

    private bool IsLocalWait(MethodWait methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
    }

    private async Task CallOwnerService(MethodWait methodWait, int pushedMethodId)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        var ownerServiceUrl =
            await _context
            .ServicesData
            .Where(x => x.AssemblyName == ownerAssemblyName)
            .Select(x => x.Url)
            .FirstOrDefaultAsync();
        //return ownerServiceUrl;
        // call "api/ResumableFunctionsReceiver/ProcessMatchedWait" for the other service with params (int waitId, int pushedMethodId)
        var actionUrl =
            $"{ownerServiceUrl}api/ResumableFunctionsReceiver/ProcessMatchedWait?waitId={methodWait.Id}&pushedMethodId={pushedMethodId}";
        using (HttpClient client = new HttpClient())
        {
            await client.GetAsync(actionUrl);
        }
    }

    //todo:like start scan
    public async Task ProcessExternalMatchedWait(int waitId, int pushedMethodId)
    {
        try
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                _context = scope.ServiceProvider.GetService<FunctionDataContext>();
                _waitsRepository = new WaitsRepository(_context);
                _metodIdsRepo = new MethodIdentifierRepository(_context);

                var methodWait = await _context
                    .MethodWaits
                    .Include(x => x.RequestedByFunction)
                    .Where(x => x.Status == WaitStatus.Waiting)
                    .FirstAsync(x => x.Id == waitId);

                var pushedMethod = await _context
                   .PushedMethodsCalls
                   .FirstAsync(x => x.Id == pushedMethodId);
            
                var externalMethod = (await _context
                    .ExternalMethodsRegistry
                    .Where(x => x.OriginalMethodHash == pushedMethod.MethodData.MethodHash)
                    .ToListAsync())
                    .FirstOrDefault(x =>
                        x.MethodData.MethodName == pushedMethod.MethodData.MethodName &&
                        x.MethodData.MethodSignature == pushedMethod.MethodData.MethodSignature);
                pushedMethod.ConvertJObject(externalMethod.MethodData.MethodInfo);
                methodWait.Input = pushedMethod.Input;
                methodWait.Output = pushedMethod.Output;

                await ProcessMatchedWait(methodWait);
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
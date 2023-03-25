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

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler
{
    internal FunctionDataContext _context;
    private WaitsRepository _waitsRepository;
    private MethodIdentifierRepository _metodIdsRepo;

    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResumableFunctionHandler> _logger;
    //public ResumableFunctionHandler(FunctionDataContext context, IBackgroundJobClient backgroundJobClient)
    //{
    //    _context = context;
    //    _waitsRepository = new WaitsRepository(_context);
    //    _metodIdsRepo = new MethodIdentifierRepository(_context);
    //    _backgroundJobClient = backgroundJobClient;
    //}
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
                foreach (var methodWait in matchedWaits)
                {
                    if (IsLocalWait(methodWait))
                    {
                        //handle if local
                        await ProcessMatchedWait(methodWait);
                    }
                    else
                    {
                        //todo: Find service owner
                        // call "api/MatchedWaitReceiver/WaitMatched" for the other service with params (waitId, pushedMethodId)
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
            WriteMessage(ex.Message);
        }
    }

    private bool IsLocalWait(MethodWait methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        string ownerAssemblyPath = $"{AppContext.BaseDirectory}{ownerAssemblyName}.dll";
        return File.Exists(ownerAssemblyPath);
    }

    private async Task<string> GetWaitOwner(MethodWait methodWait)
    {
        var ownerAssemblyName = methodWait.RequestedByFunction.AssemblyName;
        var ownerServiceUrl =
            await _context
            .ServicesData
            .Where(x => x.AssemblyName == ownerAssemblyName)
            .Select(x => x.Url)
            .FirstOrDefaultAsync();
        return ownerServiceUrl;
    }

    //todo:like start scan
    public async Task ProcessMatchedWait(int waitId, int pushedMethodId)
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
            //todo:convert pushed method input and output 
            //Get MethodInfo and use it
            //If assembly name not the current then search for external methods marked with [ExternalWaitMethodAttribute] that match
            await ProcessMatchedWait(methodWait);
        }
    }

    internal void SetDependencies(IServiceProvider serviceProvider)
    {
        _context = serviceProvider.GetService<FunctionDataContext>();
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
    }

}
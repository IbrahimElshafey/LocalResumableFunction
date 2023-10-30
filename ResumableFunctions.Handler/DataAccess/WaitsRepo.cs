using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace ResumableFunctions.Handler.DataAccess;
internal partial class WaitsRepo : IWaitsRepo
{
    private readonly ILogger<WaitsRepo> _logger;
    private readonly WaitsDataContext _context;
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly IMethodIdsRepo _methodIdsRepo;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;
    private readonly IServiceRepo _serviceRepo;

    public WaitsRepo(
        ILogger<WaitsRepo> logger,
        IBackgroundProcess backgroundJobClient,
        WaitsDataContext context,
        IMethodIdsRepo methodIdentifierRepo,
        IResumableFunctionsSettings settings,
        IWaitTemplatesRepo waitTemplatesRepo,
        IServiceRepo serviceRepo)
    {
        _logger = logger;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _methodIdsRepo = methodIdentifierRepo;
        _settings = settings;
        _waitTemplatesRepo = waitTemplatesRepo;
        _serviceRepo = serviceRepo;
    }

    public async Task<CallEffection> GetCallEffectionInCurrentService(string methodUrn)
    {
        var methodGroup = await GetMethodGroup(methodUrn);
        var affectedFunctions =
            await
            _context.MethodWaits
            .Where(x =>
                x.Status == WaitStatus.Waiting &&
                x.MethodGroupToWaitId == methodGroup.Id &&
                x.ServiceId == _settings.CurrentServiceId)
            .Select(x => x.RequestedByFunctionId)
            .Distinct()
            .ToListAsync();
        return affectedFunctions.Any() ?
            new CallEffection
            {
                AffectedServiceId = _settings.CurrentServiceId,
                AffectedServiceUrl = string.Empty,
                AffectedServiceName = _settings.CurrentServiceName,
                MethodGroupId = methodGroup.Id,
                AffectedFunctionsIds = affectedFunctions,
            }
            : null;
    }

    public async Task<List<CallEffection>> GetAffectedServicesAndFunctions(string methodUrn)
    {
        var methodGroup = await GetMethodGroup(methodUrn);

        var methodWaitsQuery = _context
                   .MethodWaits
                   .Where(x =>
                       x.Status == WaitStatus.Waiting &&
                       x.MethodGroupToWaitId == methodGroup.Id);

        if (methodGroup.IsLocalOnly)
        {
            methodWaitsQuery = methodWaitsQuery.Where(x => x.ServiceId == _settings.CurrentServiceId);
        }

        var affectedFunctionsGroupedByService =
            await methodWaitsQuery
           .Select(x => new { x.RequestedByFunctionId, x.ServiceId })
           .Distinct()
           .GroupBy(x => x.ServiceId)
           .ToListAsync();

        return (
              from service in await _context.ServicesData.Where(x => x.ParentId == -1).ToListAsync()
              from affectedFunction in affectedFunctionsGroupedByService
              where service.Id == affectedFunction.Key
              select new CallEffection
              {
                  AffectedServiceId = service.Id,
                  AffectedServiceUrl = service.Url,
                  AffectedServiceName = service.AssemblyName,
                  MethodGroupId = methodGroup.Id,
                  AffectedFunctionsIds = affectedFunction.Select(x => x.RequestedByFunctionId).ToList(),
              }
              )
              .ToList();
    }


    private async Task<MethodsGroup> GetMethodGroup(string methodUrn)
    {
        var methodGroup =
           await _context
               .MethodsGroups
               .AsNoTracking()
               .Where(x => x.MethodGroupUrn == methodUrn)
               .FirstOrDefaultAsync();
        if (methodGroup != default)
            return methodGroup;
        var error = $"Method [{methodUrn}] is not registered in current database as [WaitMethod].";
        _logger.LogWarning(error);
        throw new Exception(error);
    }

    public async Task RemoveFirstWaitIfExist(int methodIdentifierId)
    {
        try
        {
            var firstWaitItems =
                 await _context.Waits
                .Where(x =>
                    x.IsFirst &&
                    x.RequestedByFunctionId == methodIdentifierId)
                .ToListAsync();

            if (firstWaitItems != null)
            {
                foreach (var wait in firstWaitItems)
                {
                    wait.IsDeleted = true;
                    if (wait is MethodWaitEntity { Name: Constants.TimeWaitName })
                    {
                        wait.LoadUnmappedProps();
                        _backgroundJobClient.Delete(wait.ExtraData.JobId);
                    }
                }
                //load entity to delete it , concurrency control token and FKs
                if (firstWaitItems.FirstOrDefault()?.FunctionStateId is int stateId)
                {
                    var functionState = await _context
                    .FunctionStates
                    .FirstAsync(x => x.Id == stateId);
                    _context.FunctionStates.Remove(functionState);
                }
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when RemoveFirstWaitIfExist for function [{methodIdentifierId}]");
        }
    }


    public async Task CancelSubWaits(long parentId, long pushedCallId)
    {
        await CancelChildWaits(parentId);

        async Task CancelChildWaits(long pId)
        {
            var waits = await _context
                .Waits
                .Where(x => x.ParentWaitId == pId && x.Status == WaitStatus.Waiting)
                .ToListAsync();

            foreach (var wait in waits)
            {
                await CancelWait(wait, pushedCallId);
                if (wait.CanBeParent)
                    await CancelChildWaits(wait.Id);
            }
        }
    }

    private async Task CancelWait(WaitEntity wait, long pushedCallId)
    {
        if (wait.ParentWait != null)//todo:traverse up to get current function
            wait.CurrentFunction = wait.ParentWait.CurrentFunction;
        wait.LoadUnmappedProps();
        //todo:if method wait and closure changed then update root tree and all child
        wait.Cancel();
        wait.CallId = pushedCallId;
        if (wait is MethodWaitEntity mw)
        {
            await PropagateClosureIfChanged(mw);

            if (mw.Name == Constants.TimeWaitName)
                _backgroundJobClient.Delete(wait.ExtraData.JobId);
        }
        //if (wait.ParentWait != null)
        //    wait.ParentWait.CurrentFunction = wait.CurrentFunction;
        //wait.FunctionState.AddLog($"Wait [{wait.Name}] canceled.");
    }

    public async Task<WaitEntity> GetWaitParent(WaitEntity wait)
    {
        if (wait?.ParentWaitId != null)
        {
            return await _context
                .Waits
                .Include(x => x.ChildWaits)
                .Include(x => x.RequestedByFunction)
                .FirstOrDefaultAsync(x => x.Id == wait.ParentWaitId);
        }
        return null;
    }


    public async Task CancelOpenedWaitsForState(int stateId)
    {
        await _context.Waits
              .Where(x => x.FunctionStateId == stateId && x.Status == WaitStatus.Waiting)
              .ExecuteUpdateAsync(x => x.SetProperty(wait => wait.Status, status => WaitStatus.Canceled));
    }

    /// <summary>
    /// Used by ReplayWaitProcessor.GetWaitToReplay
    /// </summary>
    /// <returns></returns>
    public async Task CancelFunctionPendingWaits(int requestedByFunctionId, int functionStateId)
    {
        var pendingRootWaits =
            await _context.Waits
            .OrderByDescending(x => x.Id)
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId &&
                x.Status == WaitStatus.Waiting &&
                x.IsRoot)
            .ToListAsync();

        foreach (var wait in pendingRootWaits)
        {
            await CancelWait(wait, -1);
            await CancelSubWaits(wait.Id, -1);
        }
    }

    public async Task<WaitEntity> GetOldWaitForReplay(ReplayRequest replayWait)
    {
        var waitToReplay =
            await _context.Waits
            .OrderByDescending(x => x.Id)
            .Include(x => x.ParentWait)
            .Include(x => x.RequestedByFunction)
            .FirstOrDefaultAsync(x =>
                x.RequestedByFunctionId == replayWait.RequestedByFunctionId &&
                x.FunctionStateId == replayWait.FunctionState.Id &&
                x.Name == replayWait.Name);

        if (waitToReplay == null)
        {
            var error =
                  $"Can't replay not exiting wait [{replayWait.Name}] in function [{replayWait?.RequestedByFunction}].";
            _logger.LogError(error);
            throw new Exception(error);
        }
        var isNode = waitToReplay.IsRoot || waitToReplay.ParentWait?.WaitType == WaitType.FunctionWait;
        if (isNode is false)
        {
            var error = $"Wait to replay [{replayWait.Name}] must be a node.";
            _logger.LogError(error);
            throw new Exception(error);
        }
        return waitToReplay;
    }


    public async Task<List<MethodWaitEntity>> GetWaitsForTemplate(
        WaitTemplate template,
        string mandatoryPart,
        params Expression<Func<MethodWaitEntity, object>>[] includes)
    {
        var query = _context
            .MethodWaits
            .Where(
                wait =>
                wait.Status == WaitStatus.Waiting &&
                //wait.MethodGroupToWaitId == template.MethodGroupId &&
                //wait.ServiceId == _settings.CurrentServiceId &&
                //wait.MethodToWaitId == template.MethodId &&
                //wait.RequestedByFunctionId == template.FunctionId &&
                wait.TemplateId == template.Id);
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        if (mandatoryPart != null)
        {
            query = query.Where(wait => wait.MandatoryPart == mandatoryPart);
        }
        return
            await query
            .OrderBy(x => x.IsFirst)
            .ToListAsync();
    }

    public async Task PropagateClosureIfChanged(WaitEntity wait)
    {
        //is root
        if (wait.ParentWait == null && wait.ParentWaitId == null) return;

        //get old JObject closure from db
        var oldClosure = await _context.Waits
            .Where(x => x.Id == wait.Id)
            .Select(x => x.Closure)
            .FirstAsync() as JObject;
        var currentClosure = wait.Closure is JObject jobjectClosure ?
            jobjectClosure :
            JObject.FromObject(wait.Closure, JsonSerializer.Create(ClosureContractResolver.Settings));
        var sameAsOld = JToken.DeepEquals(oldClosure, currentClosure);
        if (sameAsOld) return;
        return;
        //all waits that have same StopPoint, RequestedBySameFunction and FunctionStateId
        //Todo: root id prefix is not accurate since we can call same method twice
        var rootId = Regex.Match(wait.Path, "^(/\\d+)/").Groups[1].Value;
        Expression<Func<WaitEntity, bool>> predicate = w =>
                w.FunctionStateId == wait.FunctionStateId &&
                w.StateAfterWait == wait.StateAfterWait &&
                w.RequestedByFunctionId == wait.RequestedByFunctionId &&
                w.CallerName == wait.CallerName &&
                w.Path.StartsWith(rootId);
        var count = await _context.Waits.Where(predicate).CountAsync();
        var waits =
            await _context.Waits
            .Where(predicate)
            .ToListAsync();
        foreach (var w in waits)
        {
            w.SetClosure(wait.Closure);
        }
    }
}
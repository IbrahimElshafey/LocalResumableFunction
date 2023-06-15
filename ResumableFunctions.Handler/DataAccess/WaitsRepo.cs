using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Helpers;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using System.Linq.Expressions;
using ResumableFunctions.Handler.Core.Abstraction;
using System.Linq;
using FastExpressionCompiler;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ResumableFunctions.Handler.DataAccess;
internal partial class WaitsRepo : IWaitsRepo
{
    private readonly ILogger<WaitsRepo> _logger;
    private readonly FunctionDataContext _context;
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly IMethodIdsRepo _methodIdsRepo;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;
    private readonly IServiceProvider _provider;

    public WaitsRepo(
        ILogger<WaitsRepo> logger,
        IBackgroundProcess backgroundJobClient,
        FunctionDataContext context,
        IMethodIdsRepo methodIdentifierRepo,
        IResumableFunctionsSettings settings,
        IWaitTemplatesRepo waitTemplatesRepo,
        IServiceProvider provider)
    {
        _logger = logger;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _methodIdsRepo = methodIdentifierRepo;
        _settings = settings;
        _waitTemplatesRepo = waitTemplatesRepo;
        _provider = provider;
    }

    public async Task<List<ServiceData>> GetAffectedServicesForCall(string methodUrn)
    {
        try
        {
            var methodGroupId = await GetMethodGroupId(methodUrn);
            var serviceIds =
                    _context
                   .MethodWaits
                   .Select(x => new { x.MethodGroupToWaitId, x.ServiceId, x.Status })
                   .Where(x => x.MethodGroupToWaitId == methodGroupId && x.Status == WaitStatus.Waiting)
                   .Distinct()
                   .Select(x => x.ServiceId)
                   //.ToList()//sqlite does not translate if in memory 
                   ;

            return await _context
                .ServicesData
                .Where(x => serviceIds.Contains(x.Id))
                .Select(x => new ServiceData { Url = x.Url, Id = x.Id })
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Error when GetServicesForMethodCall(methodUrn:{methodUrn})", ex);
            throw;
        }
    }

    public async Task<List<WaitId>> GetWaitsIdsForMethodCall(int pushedCallId, string methodUrn)
    {
        try
        {
            var matchedWaitsIds = new List<WaitId>();
            await foreach (var queryClause in GetQueryClauses(pushedCallId, methodUrn))
            {
                var matchedIds = await _context
                    .MethodWaits
                    .Where(queryClause.Clause)
                    .Select(x => new WaitId(x.Id, x.RequestedByFunctionId, x.FunctionStateId))
                    .ToListAsync();
                foreach (var waitId in matchedIds)
                {
                    waitId.FullMatch = queryClause.MakeFullMatch;
                    matchedWaitsIds.Add(waitId);
                }
            }
            //should I lock based on methodGroupId for cross services, may be no because it's used inside lock!!!




            bool noMatchedWaits = matchedWaitsIds?.Any() is not true;
            if (noMatchedWaits)
            {
                _logger.LogWarning($"No waits matched for pushed method [{pushedCallId}]");
                //_context.PushedCalls.Remove(pushedCall);
            }
            else
            {
                var waitsForCall = matchedWaitsIds
                    .Select(waitId =>
                    new WaitForCall
                    {
                        PushedCallId = pushedCallId,
                        WaitId = waitId.Id,
                        ServiceId = _settings.CurrentServiceId,
                        FunctionId = waitId.FunctionId,
                        StateId = waitId.StateId,
                        Status = waitId.FullMatch ? WaitForCallStatus.Matched : WaitForCallStatus.PartiallyMatched
                    }).ToList();
                _context.WaitsForCalls.AddRange(waitsForCall);
            }

            await _context.SaveChangesAsync();
            return matchedWaitsIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Error when GetWaitsIdsForMethodCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})", ex);
            throw;
        }
    }

    private async IAsyncEnumerable<(Expression<Func<MethodWait, bool>> Clause, bool MakeFullMatch)> GetQueryClauses(
        int pushedCallId, string methodUrn)
    {
        var pushedCall = await _context
            .PushedCalls
            .Select(x => new PushedCall { Id = x.Id, DataValue = x.DataValue })
            .FirstOrDefaultAsync(x => x.Id == pushedCallId);

        if (pushedCall is null)
            throw new NullReferenceException($"No pushed method with ID [{pushedCallId}] exist in DB.");

        var methodGroupId = await GetMethodGroupId(methodUrn);
        var templates = await _waitTemplatesRepo.GetWaitTemplates(methodGroupId);

        foreach (var template in templates)
        {
            if (template.CallMandatoryPartExpression != null)
            {
                var inputType = template.CallMandatoryPartExpression.Parameters[0].Type;
                var outputType = template.CallMandatoryPartExpression.Parameters[1].Type;
                var methodData = PushedCall.GetMethodData(inputType, outputType, pushedCall.DataValue);
                var getMandatoryFunc = template.CallMandatoryPartExpression.CompileFast();
                var parts = (string[])getMandatoryFunc.DynamicInvoke(methodData.Input, methodData.Output);
                var mandatory = string.Join("#", parts ?? new[] { "" });
                Expression<Func<MethodWait, bool>> query = x =>
                    x.MethodGroupToWaitId == methodGroupId &&
                    x.Status == WaitStatus.Waiting &&
                    x.ServiceId == _settings.CurrentServiceId &&
                    x.MethodToWaitId == template.MethodId &&
                    x.RequestedByFunctionId == template.FunctionId &&
                    x.MandatoryPart == mandatory;
                yield return (query, template.IsMandatoryPartFullMatch);
            }
            else
            {
                Expression<Func<MethodWait, bool>> query = x =>
                    x.MethodGroupToWaitId == methodGroupId &&
                    x.Status == WaitStatus.Waiting &&
                    x.ServiceId == _settings.CurrentServiceId &&
                    x.RequestedByFunctionId == template.FunctionId &&
                    x.MethodToWaitId == template.MethodId;
                yield return (query, false);
            }
        }
    }

    private async Task<int> GetMethodGroupId(string methodUrn)
    {
        var methodGroup =
           await _context
               .MethodsGroups
               .Where(x => x.MethodGroupUrn == methodUrn)
               .Select(x => x.Id)
               .FirstOrDefaultAsync();
        if (methodGroup != default)
            return methodGroup;
        else
        {
            var error = $"Method [{methodUrn}] is not registered in current database as [WaitMethod].";
            _logger.LogWarning(error);
            throw new Exception(error);
        }
    }

    public async Task<Wait> GetWaitGroup(int? parentGroupId)
    {
        var result = await _context.Waits
            .Include(x => x.ChildWaits)
            .FirstOrDefaultAsync(x => x.Id == parentGroupId);
        return result!;
    }

    private IQueryable<Wait> GetFunctionInstanceWaits(int requestedByFunctionId, int functionStateId)
    {
        return _context.Waits
            .OrderByDescending(x => x.Id)
            .Include(x => x.RequestedByFunction)
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId);
    }


    //todo:fix this for group
    public async Task RemoveFirstWaitIfExist(int methodIdentifierId)
    {
        try
        {
            var firstWaitInDb = await LoadWaitTree(
                x =>
                   x.IsFirst &&
                   x.RequestedByFunctionId == methodIdentifierId &&
                   x.Status == WaitStatus.Waiting);

            if (firstWaitInDb != null)
            {
                firstWaitInDb.ActionOnWaitsTree(wait => wait.IsDeleted = true);
                //load entity to delete it , concurrency controltoken and FKs
                var functionState = await _context
                    .FunctionStates
                    .FirstAsync(x => x.Id == firstWaitInDb.FunctionStateId);
                _context.FunctionStates.Remove(functionState);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when RemoveFirstWaitIfExist for function `{methodIdentifierId}`", ex);
        }

    }


    public async Task CancelSubWaits(int parentId)
    {
        await CancelWaits(parentId);

        async Task CancelWaits(int pId)
        {
            var waits = await _context
                .Waits
                .Include(x => x.FunctionState)
                .Where(x => x.ParentWaitId == pId && x.Status == WaitStatus.Waiting)
                .ToListAsync();
            foreach (var wait in waits)
            {
                CancelWait(wait);
                if (wait.CanBeParent)
                    await CancelWaits(wait.Id);
            }
        }
    }

    private void CancelWait(Wait wait)
    {
        wait.LoadUnmappedProps();
        wait.Cancel();
        if (wait is MethodWait mw &&
            mw.Name == $"#{nameof(LocalRegisteredMethods.TimeWait)}#")
        {
            _backgroundJobClient.Delete(wait.ExtraData.JobId);
        }
        wait.FunctionState.AddLog($"Wait `{wait.Name}` canceled.");
    }

    public async Task<Wait> GetWaitParent(Wait wait)
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

    public async Task CancelFunctionWaits(int requestedByFunctionId, int functionStateId)
    {

        //todo:doeswe handle sub functions waits
        var functionWaits =
            await GetFunctionInstanceWaits(
                    requestedByFunctionId,
                    functionStateId)
                .ToListAsync();

        foreach (var wait in functionWaits)
        {
            wait.Cancel();
            await CancelSubWaits(wait.Id);
        }
    }

    public async Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait)
    {
        var waitToReplay =
            await GetFunctionInstanceWaits(
                    replayWait.RequestedByFunctionId,
                    replayWait.FunctionState.Id)
                .FirstOrDefaultAsync(x => x.Name == replayWait.Name && x.IsNode);

        if (waitToReplay == null)
        {
            Console.WriteLine(
                $"Can't replay not exiting wait [{replayWait.Name}] in function [{replayWait?.RequestedByFunction}].");
            return null;
        }
        await _context.SaveChangesAsync();
        return waitToReplay;
    }

    public async Task<Wait> LoadWaitTree(Expression<Func<Wait, bool>> expression)
    {
        var rootId =
            await _context
            .Waits
            .Where(expression)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (rootId != default)
        {
            var waits =
                await _context
                .Waits
                .Where(x => x.Path.StartsWith($"/{rootId}"))
                .ToListAsync();
            return waits.First(x => x.Id == rootId);
        }
        return null;
    }
}
using System.Linq.Expressions;
using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;
internal partial class WaitsRepo : IWaitsRepo
{
    private readonly ILogger<WaitsRepo> _logger;
    private readonly WaitsDataContext _context;
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly IMethodIdsRepo _methodIdsRepo;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IWaitTemplatesRepo _waitTemplatesRepo;

    public WaitsRepo(
        ILogger<WaitsRepo> logger,
        IBackgroundProcess backgroundJobClient,
        WaitsDataContext context,
        IMethodIdsRepo methodIdentifierRepo,
        IResumableFunctionsSettings settings,
        IWaitTemplatesRepo waitTemplatesRepo)
    {
        _logger = logger;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _methodIdsRepo = methodIdentifierRepo;
        _settings = settings;
        _waitTemplatesRepo = waitTemplatesRepo;
    }

    public async Task<List<ServiceData>> GetAffectedServicesForCall(string methodUrn)
    {
        try
        {
            if (methodUrn.StartsWith("LocalRegisteredMethods."))
            {
                return new List<ServiceData> { new() { Id = _settings.CurrentServiceId } };
            }

            var methodGroupId = await GetMethodGroupId(methodUrn);
            var serviceIds =
                _context
                .WaitTemplates
                .Select(x => new { x.MethodGroupId, x.ServiceId })
                .Where(x => x.MethodGroupId == methodGroupId)
                .Distinct()
                .Select(x => x.ServiceId);


            return await _context
                .ServicesData
                .Where(x => serviceIds.Contains(x.Id))
                .Select(x => new ServiceData { Url = x.Url, Id = x.Id })
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Error when GetServicesForMethodCall(methodUrn:{methodUrn})");
            throw;
        }
    }

    public async Task<List<int>> GetMatchedFunctionsForCall(int pushedCallId, string methodUrn)
    {
        try
        {
            var matchedWaitsIds = new List<WaitForCall>();
            var pushedCall = await _context
                .PushedCalls
                .Select(x => new PushedCall { Id = x.Id, DataValue = x.DataValue })
                .FirstOrDefaultAsync(x => x.Id == pushedCallId);
            if (pushedCall is null)
                throw new NullReferenceException($"No pushed method with ID [{pushedCallId}] exist in DB.");

            await foreach (var queryClause in GetQueryClauses(pushedCall, methodUrn))
            {
                var matchedIds = await _context
                    .MethodWaits
                    .Where(queryClause.Clause)
                    .OrderBy(x => x.IsFirst)
                    .Select(x => new WaitForCall
                    {
                        WaitId = x.Id,
                        FunctionId = x.RequestedByFunctionId,
                        StateId = x.FunctionStateId
                    })
                    .ToListAsync();
                for (var index = 0; index < matchedIds.Count; index++)
                {
                    var waitForCall = matchedIds[index];
                    waitForCall.PushedCallId = pushedCallId;
                    waitForCall.MatchStatus = MatchStatus.PartiallyMatched;


                    if (queryClause.MakeFullMatch && index == 0)
                    {
                        matchedWaitsIds.Add(waitForCall);
                        break;
                    }

                    matchedWaitsIds.Add(waitForCall);
                }
            }

            var noMatchedWaits = matchedWaitsIds.Any() is not true;
            if (noMatchedWaits)
                _logger.LogWarning($"No waits matched for pushed method [{pushedCallId}]");

            _context.WaitsForCalls.AddRange(matchedWaitsIds);
            await _context.SaveChangesAsync();
            var functionIds = matchedWaitsIds.Select(x => x.FunctionId).Distinct().ToList();
            return functionIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when GetMatchedFunctionsForCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})");
            throw;
        }
    }

    private async IAsyncEnumerable<(Expression<Func<MethodWait, bool>> Clause, bool MakeFullMatch)> GetQueryClauses(
        PushedCall pushedCall, string methodUrn)
    {
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
                var parts = (object[])getMandatoryFunc.DynamicInvoke(methodData.Input, methodData.Output);
                var mandatory = string.Join("#", parts);
                Expression<Func<MethodWait, bool>> query = wait =>
                    wait.MethodGroupToWaitId == methodGroupId &&
                    wait.Status == WaitStatus.Waiting &&
                    wait.ServiceId == _settings.CurrentServiceId &&
                    wait.MethodToWaitId == template.MethodId &&
                    wait.RequestedByFunctionId == template.FunctionId &&
                    wait.MandatoryPart == mandatory;
                yield return (query, template.IsMandatoryPartFullMatch);
            }
            else
            {
                Expression<Func<MethodWait, bool>> query = wait =>
                    wait.MethodGroupToWaitId == methodGroupId &&
                    wait.Status == WaitStatus.Waiting &&
                    wait.ServiceId == _settings.CurrentServiceId &&
                    wait.RequestedByFunctionId == template.FunctionId &&
                    wait.MethodToWaitId == template.MethodId;
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
        var error = $"Method [{methodUrn}] is not registered in current database as [WaitMethod].";
        _logger.LogWarning(error);
        throw new Exception(error);
    }

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
                firstWaitInDb.ActionOnWaitsTree(wait =>
                {
                    wait.IsDeleted = true;
                    if (wait is MethodWait { Name: $"#{nameof(LocalRegisteredMethods.TimeWait)}#" })
                    {
                        wait.LoadUnmappedProps();
                        _backgroundJobClient.Delete(wait.ExtraData.JobId);
                    }
                });
                //load entity to delete it , concurrency control token and FKs
                var functionState = await _context
                    .FunctionStates
                    .FirstAsync(x => x.Id == firstWaitInDb.FunctionStateId);
                _context.FunctionStates.Remove(functionState);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when RemoveFirstWaitIfExist for function `{methodIdentifierId}`");
        }

    }


    public async Task CancelSubWaits(int parentId, int pushedCallId)
    {
        await CancelWaits(parentId);

        async Task CancelWaits(int pId)
        {
            var waits = await _context
                .Waits
                .Where(x => x.ParentWaitId == pId && x.Status == WaitStatus.Waiting)
                .ToListAsync();

            foreach (var wait in waits)
            {
                CancelWait(wait, pushedCallId);
                if (wait.CanBeParent)
                    await CancelWaits(wait.Id);
            }
        }
    }

    private void CancelWait(Wait wait, int pushedCallId)
    {
        wait.LoadUnmappedProps();
        wait.Cancel();
        wait.CallId = pushedCallId;
        if (wait is MethodWait { Name: $"#{nameof(LocalRegisteredMethods.TimeWait)}#" })
        {
            _backgroundJobClient.Delete(wait.ExtraData.JobId);
        }
        //wait.FunctionState.AddLog($"Wait `{wait.Name}` canceled.");
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
        var functionInstanceWaits =
            await _context.Waits
            .OrderByDescending(x => x.Id)
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId)
            .ToListAsync();

        foreach (var wait in functionInstanceWaits)
        {
            wait.Cancel();
            await CancelSubWaits(wait.Id, -1);
        }
    }

    public async Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait)
    {
        var waitToReplay =
            await _context.Waits
            .OrderByDescending(x => x.Id)
            .Include(x => x.RequestedByFunction)
            .FirstOrDefaultAsync(x =>
                x.RequestedByFunctionId == replayWait.RequestedByFunctionId &&
                x.FunctionStateId == replayWait.FunctionState.Id &&
                x.Name == replayWait.Name &&
                x.IsNode);

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
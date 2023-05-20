using System.Diagnostics;
using System.Linq;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using ResumableFunctions.Handler.DataAccess.Abstraction;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitsRepository : IWaitsRepository
{
    private ILogger<WaitsRepository> _logger;
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly FunctionDataContext _context;

    public WaitsRepository(
        ILogger<WaitsRepository> logger,
        IBackgroundJobClient backgroundJobClient,
        FunctionDataContext context) : base()
    {
        _logger = logger;
        this.backgroundJobClient = backgroundJobClient;
        _context = context;
    }

    public Task AddWait(Wait wait)
    {
        var isExistLocal = _context.Waits.Local.Contains(wait);
        var notAddStatus = _context.Entry(wait).State != EntityState.Added;
        if (isExistLocal || !notAddStatus) return Task.CompletedTask;

        Console.WriteLine($"==> Add Wait [{wait.Name}] with type [{wait.WaitType}]");
        if (wait is WaitsGroup waitGroup)
        {
            waitGroup.ChildWaits.RemoveAll(x => x is TimeWait);
        }
        if (wait is MethodWait { MethodToWaitId: > 0 } methodWait)
        {
            //does this may cause a problem
            methodWait.MethodToWait = null;
        }
        _context.Waits.Add(wait);
        return Task.CompletedTask;
    }

    public async Task<List<WaitId>> GetWaitsIdsForMethodCall(int pushedCallId)
    {

        try
        {
            var pushedCall = await _context.PushedCalls.FindAsync(pushedCallId);
            if (pushedCall == null)
                throw new NullReferenceException($"No pushed method with ID [{pushedCallId}] exist in DB.");

            var methodGroupId =
                await GetMethodGroupId(pushedCall.MethodData.MethodUrn);


            var matchedWaitsIds = await _context
                            .MethodWaits
                            .Include(x => x.RequestedByFunction)
                            //.Include(x => x.MethodToWait)
                            .Where(x =>
                                x.MethodGroupToWaitId == methodGroupId &&
                                x.Status == WaitStatus.Waiting &&
                                x.RefineMatchModifier == pushedCall.RefineMatchModifier)
                            .Select(x => new WaitId(x.Id, x.RequestedByFunction.AssemblyName))
                            .ToListAsync();



            bool noMatchedWaits = matchedWaitsIds?.Any() is not true;
            if (noMatchedWaits)
            {
                _logger.LogWarning($"No waits matched for pushed method [{pushedCallId}]");
                _context.PushedCalls.Remove(pushedCall);
            }
            else
                pushedCall.WaitsForCall = matchedWaitsIds
                    .Select(waitId =>
                    new WaitForCall
                    {
                        PushedCallId = pushedCallId,
                        WaitId = waitId.Id
                    }).ToList();

            await _context.SaveChangesAsync();
            return matchedWaitsIds;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<int> GetMethodGroupId(string methodUrn)
    {
        var waitMethodIdentifier =
           await _context
               .MethodsGroups
               .Where(x => x.MethodGroupUrn == methodUrn)
               .Select(x => x.Id)
               .FirstOrDefaultAsync();
        if (waitMethodIdentifier != default)
            return waitMethodIdentifier;
        else
        {
            _logger.LogWarning($"Method [{methodUrn}] is not registered in current database as [WaitMethod].");
            return default;
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

    public async Task RemoveFirstWaitIfExist(Wait firstWait, MethodIdentifier methodIdentifier)
    {
        try
        {
            var firstWaitInDb =
           await _context.Waits
           .FirstOrDefaultAsync(x =>
                   x.IsFirst &&
                   x.RequestedByFunctionId == methodIdentifier.Id &&
                   x.Name == firstWait.Name &&
                   x.Status == WaitStatus.Waiting);

            if (firstWaitInDb != null)
            {
                _context.Waits.Remove(firstWaitInDb);
                firstWaitInDb.CascadeAction(x => _context.Waits.Remove(x));
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
            _logger.LogError($"Error when RemoveFirstWaitIfExist {firstWait}", ex);
        }

    }


    public async Task CancelSubWaits(int parentId)
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
                CancelWait(wait);
                if (wait.CanBeParent)
                    await CancelWaits(wait.Id);
            }
        }
    }

    private void CancelWait(Wait wait)
    {
        wait.Cancel();
        if (wait is MethodWait mw &&
            mw.Name == $"#{nameof(LocalRegisteredMethods.TimeWait)}#" &&
            mw.ExtraData is JObject waitDataJson)
        {
            var waitData = waitDataJson.ToObject<TimeWaitData>();
            backgroundJobClient.Delete(waitData.JobId);
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
}
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

namespace ResumableFunctions.Handler.Data;

internal class WaitsRepository : RepositoryBase
{

    public WaitsRepository(FunctionDataContext ctx) : base(ctx)
    {
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

    public async Task<List<MethodWait>> GetMethodActiveWaits(int pushedMethodId)
    {

        try
        {
            var pushedMethod = await _context.PushedMethodsCalls.FindAsync(pushedMethodId);
            if (pushedMethod == null) return null;
            var metodIdsRepo = new MethodIdentifierRepository(_context);
            var methodGroupId =
                await metodIdsRepo.GetMethodGroupId(pushedMethod.MethodData.MethodUrn);


            var matchedWaits = await _context
                            .MethodWaits
                            .Include(x => x.RequestedByFunction)
                            .Include(x => x.MethodToWait)
                            //.Include(x => x.WaitMethodGroup)
                            .Where(x =>
                                x.WaitMethodGroupId == methodGroupId &&
                                x.Status == WaitStatus.Waiting &&
                                x.RefineMatchModifier == pushedMethod.RefineMatchModifier)
                            .ToListAsync();


            matchedWaits.ForEach(x =>
            {
                x.Input = pushedMethod.Input;
                x.Output = pushedMethod.Output;
                x.PushedMethodCallId = pushedMethodId;
            });

            bool noMatchedWaits = matchedWaits?.Any() is not true;
            if (noMatchedWaits)
            {
                //_logger.LogInformation($"No waits matched for pushed method [{pushedMethodId}]");
                //_context.PushedMethodsCalls.Remove(pushedMethod);
            }
            else
                pushedMethod.MatchedWaitsCount = matchedWaits.Count;

            await _context.SaveChangesAsync();
            return matchedWaits;
        }
        catch (Exception ex)
        {
            throw;
        }
    }



    public async Task<Wait> GetWaitGroup(int? parentGroupId)
    {
        var result = await _context.Waits
            .Include(x => x.ChildWaits)
            .FirstOrDefaultAsync(x => x.Id == parentGroupId);
        return result!;
    }

    internal IQueryable<Wait> GetFunctionInstanceWaits(int requestedByFunctionId, int functionStateId)
    {
        return _context.Waits
            .OrderByDescending(x => x.Id)
            .Include(x => x.RequestedByFunction)
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId);
    }

    internal async Task<bool> RemoveFirstWaitIfExist(Wait firstWait, MethodIdentifier methodIdentifier)
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
            _context.FunctionStates.Remove(new ResumableFunctionState { Id = firstWaitInDb.FunctionStateId });
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
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
                wait.Cancel();
                if (wait.CanBeParent)
                    await CancelWaits(wait.Id);
            }
        }
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

    public async Task<T> ReloadChildWaits<T>(T wait) where T : Wait
    {
        wait.ChildWaits = await _context.Waits.Where(x => x.ParentWaitId == wait.Id).ToListAsync();
        return wait;
    }

    public async Task CancelOpenedWaitsForState(int stateId)
    {
        await _context.Waits
              .Where(x => x.FunctionStateId == stateId && x.Status == WaitStatus.Waiting)
              .ExecuteUpdateAsync(x => x.SetProperty(wait => wait.Status, status => WaitStatus.Canceled));
    }

    public async Task CancelFunctionWaits(int requestedByFunctionId, int functionStateId)
    {
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

    internal async Task<Wait> GetOldWaitForReplay(ReplayRequest replayWait)
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
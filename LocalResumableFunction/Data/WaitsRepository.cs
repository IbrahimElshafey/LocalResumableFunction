using System.Diagnostics;
using System.Linq;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace LocalResumableFunction.Data;

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
        if (wait is MethodWait { WaitMethodIdentifierId: > 0 } methodWait)
        {
            //does this may cause a problem
            methodWait.WaitMethodIdentifier = null;
        }
        _context.Waits.Add(wait);
        return Task.CompletedTask;
    }

    public async Task<List<MethodWait>> GetMatchedWaits(PushedMethod pushedMethod)
    {
        var matchedWaits = new List<MethodWait>();
        var databaseWaits =
            await _context
                .MethodWaits
                .Include(x => x.RequestedByFunction)
                .Where(x =>
                    x.WaitMethodIdentifierId == pushedMethod.MethodIdentifier.Id &&
                    x.Status == WaitStatus.Waiting)
                .ToListAsync();
        databaseWaits.ForEach(wait => wait.LoadExpressions());
        foreach (var methodWait in databaseWaits)
        {
            methodWait.Input = pushedMethod.Input;
            methodWait.Output = pushedMethod.Output;
            switch (methodWait.NeedFunctionStateForMatch)
            {
                case false when methodWait.CheckMatch():
                    await LoadWaitFunctionState(methodWait);
                    matchedWaits.Add(methodWait);
                    break;
                case true:
                    {
                        await LoadWaitFunctionState(methodWait);
                        if (methodWait.CheckMatch())
                            matchedWaits.Add(methodWait);
                        break;
                    }
            }
        }

        return matchedWaits;

        async Task LoadWaitFunctionState(MethodWait wait)
        {
            wait.FunctionState = await _context.FunctionStates.FindAsync(wait.FunctionStateId);
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
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId);
    }

    internal Task<bool> FirstWaitExistInDb(Wait firstWait, MethodIdentifier methodIdentifier)
    {
        return _context.Waits.AnyAsync(x =>
            x.IsFirst &&
            x.RequestedByFunctionId == methodIdentifier.Id &&
            x.Name == firstWait.Name &&
            x.Status == WaitStatus.Waiting);
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
                wait.Status = WaitStatus.Canceled;
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
            if (wait.Status == WaitStatus.Waiting)
                wait.Status = WaitStatus.Canceled;
            await CancelSubWaits(wait.Id);
        }
    }

    internal async Task<Wait> GetOldWaitForReplay(ReplayWait replayWait)
    {
        var waitToReplay =
            await GetFunctionInstanceWaits(
                    replayWait.RequestedByFunctionId,
                    replayWait.FunctionState.Id)
                .FirstOrDefaultAsync(x => x.Name == replayWait.Name && x.IsNode);

        if (waitToReplay == null)
        {
            Console.WriteLine(
                $"Can't replay not exiting wait [{replayWait.Name}] in function [{replayWait?.RequestedByFunction?.MethodName}].");
            return null;
        }
        await _context.SaveChangesAsync();
        return waitToReplay;
    }
}
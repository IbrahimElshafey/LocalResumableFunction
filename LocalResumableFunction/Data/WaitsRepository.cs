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

    internal async Task<List<Wait>> GetFunctionInstanceWaits(int requestedByFunctionId, int functionStateId)
    {
        var result = await _context.Waits
            .OrderByDescending(x => x.Id)
            .Where(x =>
                x.RequestedByFunctionId == requestedByFunctionId &&
                x.FunctionStateId == functionStateId)
            .ToListAsync();
        //&& x.ParentWaitId == replayWait.ParentWaitId
        return result!;
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
}
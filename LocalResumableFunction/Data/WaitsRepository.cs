using System.Diagnostics;
using System.Linq;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

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
        if (isExistLocal is false && notAddStatus)
        {
            Console.WriteLine($"==> Add Wait [{wait.Name}] with type [{wait.WaitType}]");
            _context.Waits.Add(wait);
        }
        return Task.CompletedTask;
    }

    public async Task<List<MethodWait>> GetMatchedWaits(PushedMethod pushedMethod)
    {
        Debugger.Launch();
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
            if (!methodWait.NeedFunctionStateForMatch && CheckMatch(methodWait, pushedMethod))
            {
                await LoadWaitFunctionState(methodWait);
                matchedWaits.Add(methodWait);
            }
            else if (methodWait.NeedFunctionStateForMatch)
            {
                await LoadWaitFunctionState(methodWait);
                if (CheckMatch(methodWait, pushedMethod))
                    matchedWaits.Add(methodWait);
            }

        return matchedWaits;

        async Task LoadWaitFunctionState(MethodWait wait)
        {
            wait.FunctionState = await _context.FunctionStates.FindAsync(wait.FunctionStateId);
        }
    }

    public async Task<(Wait? RootWait, int? MethodGroupId, int? FunctionWaitId, int? FunctionGroupId)> GetRootFunctionWait(int? parentId)
    {
        var result = (RootWait: default(Wait), MethodGroupId: default(int?), FunctionWaitId: default(int?), FunctionGroupId: default(int?));
        Debugger.Launch();
        var rootWait = await _context.Waits.FirstOrDefaultAsync(x => x.Id == parentId);

        if (rootWait is ManyMethodsWait && rootWait.ParentWaitId != null)
        {
            result.MethodGroupId = rootWait.Id;
            rootWait = await _context.Waits.FirstOrDefaultAsync(x => x.Id == rootWait.ParentWaitId);
        }

        if (rootWait is FunctionWait && rootWait.ParentWaitId != null)
        {
            result.FunctionWaitId = rootWait.Id;
            result.FunctionGroupId = rootWait.ParentWaitId;
            rootWait = await _context.Waits.FirstOrDefaultAsync(x => x.Id == rootWait.ParentWaitId);
        }

        if (rootWait is not null)
        {
            await _context.Entry(rootWait).Reference(x => x.RequestedByFunction).LoadAsync();
            await _context.Entry(rootWait).Collection(x => x.ChildWaits).LoadAsync();
        }
        result.RootWait = rootWait;
        return result;
    }

    public async Task<Wait?> GetWaitGroup(int? parentGroupId)
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

    private bool CheckMatch(MethodWait methodWait, PushedMethod pushedMethod)
    {
        try
        {
            if (methodWait.IsFirst && methodWait.MatchIfExpressionValue == null)
                return true;
            var check = methodWait.MatchIfExpression.Compile();
            return (bool)check.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, methodWait.CurrntFunction);
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task CancelAllWaits(ManyFunctionsWait anyFunctionWait)
    {
        var functionIds = anyFunctionWait
            .WaitingFunctions
            .Where(x => x.ParentWaitId != null)
            .Select(x => x.Id)
            .ToList();
        var waitsToCancel = await _context
            .Waits
            .Where(x => functionIds.Contains((int)x.ParentWaitId!))
            .ToListAsync();
        foreach (var wait in waitsToCancel)
        {
            if (wait.Status == WaitStatus.Waiting)
                wait.Status = WaitStatus.Canceled;
        }
    }
}
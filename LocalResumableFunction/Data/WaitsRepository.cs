using System.Diagnostics;
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
        var isExistLocal = Context.Waits.Local.Contains(wait);
        var notAddStatus = Context.Entry(wait).State != EntityState.Added;
        if (isExistLocal is false && notAddStatus)
        {
            Console.WriteLine($"==> Add Wait [{wait.Name}] with type [{wait.WaitType}]");
            Context.Waits.Add(wait);
        }
        return Task.CompletedTask;
    }

    public async Task<List<MethodWait>> GetMatchedWaits(PushedMethod pushedMethod)
    {
        Debugger.Launch();
        var matchedWaits = new List<MethodWait>();
        var databaseWaits =
            await Context
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
            wait.FunctionState = await Context.FunctionStates.FindAsync(wait.FunctionStateId);
        }
    }

    public async Task<Wait> GetParentFunctionWait(int? functionWaitId)
    {
        Debugger.Launch();
        var result = await Context.FunctionWaits
            .Include(x => x.RequestedByFunction)
            .FirstAsync(x => x.Id == functionWaitId);
        if (result != null && result.ParentFunctionGroupId != null)
        {
            var manyFunc = await Context.ManyFunctionsWaits
                .Include(x => x.WaitingFunctions)
                .Include(x => x.RequestedByFunction)
                .FirstOrDefaultAsync(x => x.Id == result.ParentFunctionGroupId);
            return manyFunc!;
        }

        return result;
    }

    public async Task<ManyMethodsWait> GetWaitGroup(int? parentGroupId)
    {
        var result = await Context.ManyMethodsWaits
            .Include(x => x.WaitingMethods)
            .FirstOrDefaultAsync(x => x.Id == parentGroupId);
        return result!;
    }

    internal async Task<List<Wait>> GetFunctionInstanceWaits(int requestedByFunctionId, int functionStateId)
    {
        var result = await Context.Waits
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
        return Context.Waits.AnyAsync(x =>
            x.IsFirst &&
            x.RequestedByFunctionId == methodIdentifier.Id &&
            x.Name == firstWait.Name &&
            x.Status == WaitStatus.Waiting);
    }

    private bool CheckMatch(MethodWait methodWait, PushedMethod pushedMethod)
    {
        try
        {
            var check = methodWait.MatchIfExpression.Compile();
            return (bool)check.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, methodWait.CurrntFunction);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}
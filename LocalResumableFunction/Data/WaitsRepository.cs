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
        Context.Waits.Add(wait);
        return Task.CompletedTask;
    }

    public async Task DuplicateWaitIfFirst(MethodWait currentWait)
    {
        if (currentWait.IsFirst)
            DuplicateMethodWait(currentWait);
        else if (currentWait.ParentWaitsGroupId is not null)
        {
            var waitGroup = await GetWaitGroup(currentWait.ParentWaitsGroupId);
            if (waitGroup.IsFirst)
                DuplicateWaitGroup(waitGroup);
        }
        else if (currentWait.ParentWaitId is not null)
        {
            var functionWait = await GetParentFunctionWait(currentWait.ParentWaitId);
            if (functionWait.IsFirst)
                DuplicateFunctionWait(functionWait);
        }

        void DuplicateMethodWait(MethodWait methodWait)
        {
            var result = new MethodWait
            {
                Status = WaitStatus.Waiting,
                Name = methodWait.Name,
                MatchIfExpressionValue = methodWait.MatchIfExpressionValue,
                SetDataExpressionValue = methodWait.SetDataExpressionValue,
                FunctionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodWait.RequestedByFunction,
                    StateObject = "{}",
                },
                IsFirst = true,
                IsNode = methodWait.IsNode,
                IsOptional = methodWait.IsOptional,
                NeedFunctionStateForMatch = methodWait.NeedFunctionStateForMatch,
                StateAfterWait = methodWait.StateAfterWait,
                WaitType = methodWait.WaitType,
                RequestedByFunction = methodWait.RequestedByFunction,
                RequestedByFunctionId = methodWait.RequestedByFunctionId,
                WaitMethodIdentifier = methodWait.WaitMethodIdentifier,
                WaitMethodIdentifierId = methodWait.WaitMethodIdentifierId
            };
            Context.MethodWaits.Add(result);
        }
        void DuplicateWaitGroup(ManyMethodsWait waitGroup)
        {

        }
        void DuplicateFunctionWait(Wait functionWait)
        {
        }
    }

    public async Task<List<MethodWait>> GetMatchedWaits(PushedMethod pushedMethod)
    {
        var matchedWaits = new List<MethodWait>();
        var databaseWaits =
            await Context.MethodWaits
                .Where(x =>
                    x.WaitMethodIdentifierId == pushedMethod.MethodIdentifier.Id &&
                    x.Status == WaitStatus.Waiting)
                .ToListAsync();
        databaseWaits.ForEach(wait => wait.GetExpressions());
        foreach (var methodWait in databaseWaits)
        {
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
        }

        return matchedWaits;

        async Task LoadWaitFunctionState(MethodWait wait)
        {
            wait.FunctionState = await Context.FunctionStates.FindAsync(wait.FunctionStateId);
        }
    }

    private bool CheckMatch(MethodWait methodWait, PushedMethod pushedMethod)
    {
        var check = methodWait.MatchIfExpression.Compile();
        return (bool)check.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, methodWait.CurrntFunction);
    }

    public async Task<Wait> GetParentFunctionWait(int? functionWaitId)
    {
        var result = await Context.FunctionWaits.FindAsync(functionWaitId);
        if (result == null)
        {
            var manyFunc = await Context.ManyFunctionsWaits
                    .Include(x => x.WaitingFunctions)
                    .FirstOrDefaultAsync(x => x.Id == functionWaitId);
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
}
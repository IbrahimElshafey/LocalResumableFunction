﻿using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.Data;

internal class WaitsRepository : RepositoryBase
{
    public WaitsRepository(EngineDataContext ctx) : base(ctx)
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
                MatchIfExpression = methodWait.MatchIfExpression,
                SetDataExpression = methodWait.SetDataExpression,
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
                    x.WaitMethodIdentifierId == pushedMethod.MethodIdentifierId &&
                    x.Status == WaitStatus.Waiting)
                .ToListAsync();
        foreach (var methodWait in databaseWaits)
            // .If((input, output) => input.ProjectId == CurrentProject.Id)
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

    private bool CheckMatch(MethodWait methodWait, PushedMethod pushedMethod)
    {
        var check = methodWait.MatchIfExpression.Compile();
        return (bool)check.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, methodWait.CurrntFunction);
    }

    public Task<Wait> GetParentFunctionWait(int? functionWaitId)
    {
        throw new NotImplementedException();
    }

    public Task<ManyMethodsWait> GetWaitGroup(int? parentGroupId)
    {
        throw new NotImplementedException();
    }
}
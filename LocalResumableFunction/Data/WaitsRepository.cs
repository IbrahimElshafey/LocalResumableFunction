using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction.Data
{
    internal class WaitsRepository
    {
        private readonly EngineDataContext _context;

        public WaitsRepository(EngineDataContext ctx) => _context = ctx;

        public Task AddWait(Wait eventWait)
        {
            throw new NotImplementedException();
        }

        public Task DuplicateWaitIfFirst(MethodWait currentWait)
        {
            throw new NotImplementedException();
        }

        public async Task<List<MethodWait>> GetMatchedWaits(PushedMethod pushedEvent)
        {
            var matchedWaits = new List<MethodWait>();
            var databaseWaits =
                await _context.MethodWaits
                .Where(x =>
                    x.WaitMethodIdentifierId == pushedEvent.MethodIdentifierId &&
                    x.Status == WaitStatus.Waiting)
                .ToListAsync();
            foreach (var methodWait in databaseWaits)
            {
                // .If((input, output) => input.ProjectId == CurrentProject.Id)
                if (!methodWait.NeedFunctionStateForMatch && CheckMatch(methodWait,pushedEvent))
                {
                    await LoadWaitFunctionState(methodWait);
                    matchedWaits.Add(methodWait);
                }
                else if (methodWait.NeedFunctionStateForMatch)
                {
                    await LoadWaitFunctionState(methodWait);
                    if (CheckMatch(methodWait, pushedEvent))
                        matchedWaits.Add(methodWait);
                }
            }
            return matchedWaits;

            async Task LoadWaitFunctionState(MethodWait wait) 
                => wait.FunctionState = await _context.FunctionStates.FindAsync(wait.FunctionStateId);
        }

        private bool CheckMatch(MethodWait methodWait,PushedMethod pushedMethod)
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
}

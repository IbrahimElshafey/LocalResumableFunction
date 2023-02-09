using LocalResumableFunction.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction.Data
{
    internal interface IWaitsRepository
    {
        Task AddWait(Wait eventWait);
        Task<Wait> GetParentFunctionWait(int? functionWaitId);
        Task<List<MethodWait>> GetMatchedWaits(PushCalledMethod pushedEvent);
        Task<ManyMethodsWait> GetWaitGroup(int? parentGroupId);
        Task DuplicateWaitIfFirst(MethodWait currentWait);
    }
}

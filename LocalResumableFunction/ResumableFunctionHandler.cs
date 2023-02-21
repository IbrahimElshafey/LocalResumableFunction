using System.Diagnostics;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private readonly FunctionDataContext _context;
    private readonly WaitsRepository _waitsRepository;
    private readonly MethodIdentifierRepository _metodIdsRepo;

    internal ResumableFunctionHandler(FunctionDataContext context = null)
    {
        _context = context ?? new FunctionDataContext();
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
    }

    private async Task DuplicateIfFirst(Wait currentWait)
    {
        if (currentWait.IsFirst)
            await RegisterFirstWait(currentWait.RequestedByFunction.MethodInfo);
    }

    private void UpdateFunctionData(MethodWait currentWait, PushedMethod pushedMethod)
    {
        var setDataExpression = currentWait.SetDataExpression.Compile();
        setDataExpression.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, currentWait.CurrntFunction);
        currentWait.FunctionState.StateObject = currentWait.CurrntFunction;
    }

    private async Task<bool> MoveFunctionToRecycleBin(Wait currentWait)
    {
        //throw new NotImplementedException();
        return true;
    }
}
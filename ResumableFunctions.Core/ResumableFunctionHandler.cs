using System.Diagnostics;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace ResumableFunctions.Core;

internal partial class ResumableFunctionHandler
{
    private readonly FunctionDataContext _context;
    private readonly WaitsRepository _waitsRepository;
    private readonly MethodIdentifierRepository _metodIdsRepo;
    private readonly IBackgroundJobClient _backgroundJobClient;

    internal ResumableFunctionHandler(FunctionDataContext context, IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _waitsRepository = new WaitsRepository(_context);
        _metodIdsRepo = new MethodIdentifierRepository(_context);
        _backgroundJobClient = backgroundJobClient;
    }

    private async Task DuplicateIfFirst(Wait currentWait)
    {
        if (currentWait.IsFirst)
            await RegisterFirstWait(currentWait.RequestedByFunction.MethodInfo);
    }

    private async Task<bool> MoveFunctionToRecycleBin(Wait lastWait)
    {
        //throw new NotImplementedException();
        return true;
    }
}
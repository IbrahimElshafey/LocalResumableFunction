﻿using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class MethodIdsRepo : IMethodIdsRepo
{
    private readonly ILogger<MethodIdsRepo> _logger;
    private readonly WaitsDataContext _context;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IResumableFunctionsSettings _settings;

    public MethodIdsRepo(
        ILogger<MethodIdsRepo> logger,
        WaitsDataContext context,
        IDistributedLockProvider lockProvider,
        IResumableFunctionsSettings settings)
    {
        _logger = logger;
        _context = context;
        _lockProvider = lockProvider;
        _settings = settings;
    }

    public async Task<ResumableFunctionIdentifier> GetResumableFunction(long id)
    {
        var resumableFunctionIdentifier =
           await _context
               .ResumableFunctionIdentifiers
               .FirstOrDefaultAsync(x => x.Id == id);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        var error = $"Can't find resumable function with ID `{id}` in database.";
        _logger.LogError(error);
        throw new NullReferenceException(error);
    }

    public async Task<ResumableFunctionIdentifier> TryGetResumableFunction(MethodData methodData)
    {
        methodData.Validate();
        return await _context
                .ResumableFunctionIdentifiers
                .FirstOrDefaultAsync(
                    x => (x.Type == MethodType.ResumableFunctionEntryPoint || x.Type == MethodType.SubResumableFunction) &&
                         x.RF_MethodUrn == methodData.MethodUrn &&
                         x.ServiceId == _settings.CurrentServiceId);
    }

    public async Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData)
    {
        var resumableFunctionIdentifier = await TryGetResumableFunction(methodData);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        var error = $"Can't find resumable function ({methodData}) in database.";
        _logger.LogError(error);
        throw new NullReferenceException(error);
    }

    public async Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData)
    {
        await using var lockHandle =
            await _lockProvider.AcquireLockAsync($"ResumableFunction_{methodData.MethodUrn}");
        var inDb = await TryGetResumableFunction(methodData);
        if (inDb != null)
        {
            inDb.FillFromMethodData(methodData);
            return inDb;
        }

        var add = new ResumableFunctionIdentifier();
        add.FillFromMethodData(methodData);
        _context.ResumableFunctionIdentifiers.Add(add);
        return add;
    }

    public async Task AddWaitMethodIdentifier(MethodData methodData)
    {
        await using var waitHandle =
            await _lockProvider.AcquireLockAsync($"MethodGroup_{methodData.MethodUrn}");
        var methodGroup =
            await _context
                .MethodsGroups
                .Include(x => x.WaitMethodIdentifiers)
                .FirstOrDefaultAsync(x => x.MethodGroupUrn == methodData.MethodUrn);
        var methodInDb = methodGroup?.WaitMethodIdentifiers?
            .FirstOrDefault(x => x.MethodHash.SequenceEqual(methodData.MethodHash));

        var isUpdate =
            methodGroup != null &&
            methodInDb != null;
        if (isUpdate)
        {
            methodInDb.FillFromMethodData(methodData);
            return;
        }


        var toAdd = new WaitMethodIdentifier();
        toAdd.FillFromMethodData(methodData);
        var isChildAdd =
            methodGroup != null;

        if (isChildAdd)
        {
            methodGroup.WaitMethodIdentifiers?.Add(toAdd);
        }
        else
        {
            var group = new MethodsGroup
            {
                MethodGroupUrn = methodData.MethodUrn,
            };
            group.WaitMethodIdentifiers.Add(toAdd);
            _context.MethodsGroups.Add(group);
            await _context.SaveChangesAsync();
        }
    }



    public async Task<(long MethodId, long GroupId)> GetId(MethodWait methodWait)
    {
        if (methodWait.MethodGroupToWaitId != default && methodWait.MethodToWaitId != default)
            return (methodWait.MethodToWaitId ?? 0, methodWait.MethodGroupToWaitId);

        var methodData = methodWait.MethodData;
        methodData.Validate();
        var groupIdQuery = _context
            .MethodsGroups
            .Where(x => x.MethodGroupUrn == methodData.MethodUrn)
            .Select(x => x.Id);

        var methodIdQry = _context
            .WaitMethodIdentifiers
            .Where(x =>
                groupIdQuery.Contains(x.MethodGroupId) &&
                x.MethodName == methodData.MethodName &&
                x.ClassName == methodData.ClassName &&
                x.AssemblyName == methodData.AssemblyName
            )
            .Select(x => new { x.Id, x.MethodGroupId });
        var methodId = await methodIdQry.FirstAsync();
        return (methodId.Id, methodId.MethodGroupId);
    }
}
﻿using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using Medallion.Threading;

namespace ResumableFunctions.Handler.DataAccess;

internal class MethodIdentifierRepository : IMethodIdentifierRepository
{
    private ILogger<MethodIdentifierRepository> _logger;
    private readonly FunctionDataContext _context;
    private readonly IDistributedLockProvider _lockProvider;

    public MethodIdentifierRepository(
        ILogger<MethodIdentifierRepository> logger,
        FunctionDataContext context,
        IDistributedLockProvider lockProvider)
    {
        _logger = logger;
        _context = context;
        _lockProvider = lockProvider;
    }

    public async Task<ResumableFunctionIdentifier> GetResumableFunction(int id)
    {
        var resumableFunctionIdentifier =
           await _context
               .ResumableFunctionIdentifiers
               .FirstOrDefaultAsync(x => x.Id == id);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        else
            throw new NullReferenceException($"Can't find resumable function with ID `{id}` in database.");
    }
    public async Task<ResumableFunctionIdentifier> TryGetResumableFunction(MethodData methodData)
    {
        methodData.Validate();
        return await _context
                .ResumableFunctionIdentifiers
                .FirstOrDefaultAsync(x => x.RF_MethodUrn == methodData.MethodUrn);
    }
    public async Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData)
    {
        var resumableFunctionIdentifier = await TryGetResumableFunction(methodData);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        else
            throw new NullReferenceException($"Can't find resumable function ({methodData}) in database.");
    }

    public async Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData, int? serviceId)
    {
        using (await _lockProvider.AcquireLockAsync($"ResumableFunction_{methodData.MethodUrn}"))
        {
            var inDb = await TryGetResumableFunction(methodData);
            if (inDb != null)
            {
                inDb.FillFromMethodData(methodData);
                inDb.ServiceId = serviceId;
                return inDb;
            }
            else
            {
                var add = new ResumableFunctionIdentifier();
                add.FillFromMethodData(methodData);
                add.ServiceId = serviceId;
                _context.ResumableFunctionIdentifiers.Add(add);
                return add;
            }
        }
    }

    public async Task AddWaitMethodIdentifier(MethodData methodData, int service)
    {
        using (await _lockProvider.AcquireLockAsync($"WaitMethod_{methodData.MethodUrn}"))
        {
            //todo:validate same signature for group methods
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
                methodInDb.ServiceId = service;
                return;
            }


            var toAdd = new WaitMethodIdentifier();
            toAdd.FillFromMethodData(methodData);
            toAdd.ServiceId = service;
            var isChildAdd =
                methodGroup != null &&
                methodInDb == null;
            var isNewParent = methodGroup == null;

            if (isChildAdd)
                methodGroup.WaitMethodIdentifiers.Add(toAdd);
            else if (isNewParent)
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

    }

    public async Task<WaitMethodIdentifier> GetWaitMethod(MethodWait methodWait)
    {
        if (methodWait.MethodData == null)
            return
                await _context
                .WaitMethodIdentifiers
                .Include(x => x.ParentMethodGroup)
                .FirstOrDefaultAsync(x => x.Id == methodWait.MethodToWaitId);

        var methodData = methodWait.MethodData;
        methodData.Validate();
        var methodGroup =
            await _context
                .MethodsGroups
                .Include(x => x.WaitMethodIdentifiers)
                .FirstOrDefaultAsync(x => x.MethodGroupUrn == methodData.MethodUrn);
        var childMethodIdentifier =
            methodGroup
            .WaitMethodIdentifiers
            .FirstOrDefault(x => x.MethodHash.SequenceEqual(methodData.MethodHash));
        if (childMethodIdentifier != null)
        {
            return childMethodIdentifier;
        }
        else
            throw new NullReferenceException($"Can't find wait method ({methodData}) in database.");
    }
}
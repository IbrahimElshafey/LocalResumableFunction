using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Handler.Data;

internal class MethodIdentifierRepository : RepositoryBase
{
    private ILogger<MethodIdentifierRepository> _logger;

    public MethodIdentifierRepository(FunctionDataContext ctx) : base(ctx)
    {
        _logger = CoreExtensions.GetServiceProvider().GetService<ILogger<MethodIdentifierRepository>>();
    }

    internal async Task<int> GetMethodGroupId(string methodUrn)
    {
        var waitMethodIdentifier =
           await _context
               .WaitMethodGroups
               .Where(x => x.MethodGroupUrn == methodUrn)
               .Select(x => x.Id)
               .FirstOrDefaultAsync();
        if (waitMethodIdentifier != default)
            return waitMethodIdentifier;
        else
        {
            _logger.LogWarning($"Method [{methodUrn}] is not registered in current database as [WaitMethod].");
            return default;
        }
    }

    internal async Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData)
    {
        methodData.Validate();
        var resumableFunctionIdentifier =
            await _context
                .ResumableFunctionIdentifiers
                .FirstOrDefaultAsync(x => x.MethodUrn == methodData.MethodUrn);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        else
        {
            _logger.LogWarning($"Can't find resumable function ({methodData}) in database.");
            return null;
        }
    }

    internal async Task AddResumableFunctionIdentifier(MethodData methodData)
    {
        var inDb = await GetResumableFunction(methodData);
        if (inDb != null)
        {
            inDb.FillFromMethodData(methodData);
        }
        else
        {
            var add = new ResumableFunctionIdentifier();
            add.FillFromMethodData(methodData);
            _context.ResumableFunctionIdentifiers.Add(add);
        }
    }

    internal async Task AddWaitMethodIdentifier(MethodData methodData)
    {
        var methodGroup =
            await _context
                .WaitMethodGroups
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
            methodGroup != null &&
            methodInDb == null;
        var isNewParent = methodGroup == null;

        if (isChildAdd)
            methodGroup.WaitMethodIdentifiers.Add(toAdd);
        else if (isNewParent)
        {
            var group = new WaitMethodGroup
            {
                MethodGroupUrn = methodData.MethodUrn,
            };
            group.WaitMethodIdentifiers.Add(toAdd);
            _context.WaitMethodGroups.Add(group);
            await _context.SaveChangesAsync();
        }

    }

    internal async Task<WaitMethodIdentifier> GetWaitMethod(MethodWait methodWait)
    {
        if (methodWait.MethodData == null)
            return
                await _context
                .WaitMethodIdentifiers
                .Include(x => x.WaitMethodGroup)
                .FirstOrDefaultAsync(x => x.Id == methodWait.MethodToWaitId);

        var methodData = methodWait.MethodData;
        methodData.Validate();
        var methodGroup =
            await _context
                .WaitMethodGroups
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
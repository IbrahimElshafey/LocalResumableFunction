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

    public MethodIdentifierRepository(ILogger<MethodIdentifierRepository> logger) : base()
    {
        _logger = logger;
    }

    

    internal async Task<ResumableFunctionIdentifier> GetResumableFunction(int id)
    {
        var resumableFunctionIdentifier =
           await _context
               .ResumableFunctionIdentifiers
               .FirstOrDefaultAsync(x => x.Id == id);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        else
        {
            _logger.LogWarning($"Can't find resumable function with ID ({id}) in database.");
            return null;
        }
    }
    internal async Task<ResumableFunctionIdentifier> GetResumableFunction(MethodData methodData)
    {
        methodData.Validate();
        var resumableFunctionIdentifier =
            await _context
                .ResumableFunctionIdentifiers
                .FirstOrDefaultAsync(x => x.RF_MethodUrn == methodData.MethodUrn);
        if (resumableFunctionIdentifier != null)
            return resumableFunctionIdentifier;
        else
        {
            _logger.LogWarning($"Can't find resumable function ({methodData}) in database.");
            return null;
        }
    }

    internal async Task<ResumableFunctionIdentifier> AddResumableFunctionIdentifier(MethodData methodData)
    {
        var inDb = await GetResumableFunction(methodData);
        if (inDb != null)
        {
            inDb.FillFromMethodData(methodData);
            return inDb;
        }
        else
        {
            var add = new ResumableFunctionIdentifier();
            add.FillFromMethodData(methodData);
            _context.ResumableFunctionIdentifiers.Add(add);
            return add;
        }
    }

    internal async Task AddWaitMethodIdentifier(MethodData methodData)
    {
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

    internal async Task<WaitMethodIdentifier> GetWaitMethod(MethodWait methodWait)
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
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class MethodIdsRepo : IMethodIdsRepo
{
    private readonly ILogger<MethodIdsRepo> _logger;
    private readonly FunctionDataContext _context;
    private readonly IDistributedLockProvider _lockProvider;

    public MethodIdsRepo(
        ILogger<MethodIdsRepo> logger,
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

            var add = new ResumableFunctionIdentifier();
            add.FillFromMethodData(methodData);
            add.ServiceId = serviceId;
            _context.ResumableFunctionIdentifiers.Add(add);
            return add;
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
        if (methodWait.MethodData == null && methodWait.MethodToWaitId != default)
            return
                await _context
                .WaitMethodIdentifiers
                .FirstOrDefaultAsync(x => x.Id == methodWait.MethodToWaitId);

        var methodData = methodWait.MethodData;
        methodData.Validate();
        //todo:refine this query
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

        throw new NullReferenceException($"Can't find wait method ({methodData}) in database.");
    }

    public async Task<(int MethodId, int GroupId)> GetId(MethodWait methodWait)
    {

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
            x.AssemblyName == methodData.AssemblyName)
            .Select(x => new { x.Id, x.MethodGroupId });
        var methodId = await methodIdQry.FirstAsync();
        return (methodId.Id, methodId.MethodGroupId);
    }
}
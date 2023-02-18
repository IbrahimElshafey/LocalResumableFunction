using System.Reflection;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.Data;

internal class MethodIdentifierRepository : RepositoryBase
{
    public MethodIdentifierRepository(FunctionDataContext ctx) : base(ctx)
    {
    }

    public async Task<(MethodIdentifier MethodIdentifier, bool ExistInDb)> GetMethodIdentifier(MethodBase methodInfo)
    {
        var methodId = new MethodIdentifier();
        methodId.SetMethodInfo(methodInfo);
        var existInDb = await GetMethodIdentifier(methodId);
        return (existInDb, existInDb.Id > 0);
    }

    public async Task<MethodIdentifier> GetMethodIdentifier(MethodIdentifier methodId)
    {
        var inDb = await Context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        if (inDb == null)
            inDb = Context
                .MethodIdentifiers
                .Local
                .Where(x => x.MethodHash == methodId.MethodHash).ToList();
        var existInDb =
            inDb.FirstOrDefault(x =>
                x.MethodSignature == methodId.MethodSignature &&
                x.AssemblyName == methodId.AssemblyName &&
                x.ClassName == methodId.ClassName &&
                x.MethodName == methodId.MethodName);
        return existInDb ?? methodId;
    }
}
using LocalResumableFunction.InOuts;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.Data;

internal class MethodIdentifierRepository : RepositoryBase
{
    public MethodIdentifierRepository(EngineDataContext ctx) : base(ctx)
    {
    }

    public async Task<(MethodIdentifier MethodIdentifier, bool ExistInDb)> GetMethodIdentifier(MethodBase methodInfo)
    {
        var methodId = new MethodIdentifier { Id = -1 };
        methodId.SetMethodBase(methodInfo);
        var inDb = await Context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        var existInDb =
            inDb.FirstOrDefault(x =>
            x.MethodSignature == methodId.MethodSignature &&
            x.AssemblyName == methodId.AssemblyName &&
            x.ClassName == methodId.ClassName &&
            x.MethodName == methodId.MethodName);
        return existInDb != null ? (existInDb, true) : (methodId, false);
    }

    public async Task<MethodIdentifier> GetMethodIdentifier(MethodIdentifier inMemoryIdentifier)
    {
        var inDb = await Context
            .MethodIdentifiers
            .Where(x => x.MethodHash == inMemoryIdentifier.MethodHash).ToListAsync();
        var existInDb =
            inDb.FirstOrDefault(x =>
                x.MethodSignature == inMemoryIdentifier.MethodSignature &&
                x.AssemblyName == inMemoryIdentifier.AssemblyName &&
                x.ClassName == inMemoryIdentifier.ClassName &&
                x.MethodName == inMemoryIdentifier.MethodName);
        return existInDb;
    }


}
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
        var methodId = new MethodIdentifier();
        methodId.SetMethodInfo(methodInfo);
        MethodIdentifier? existInDb = await GetMethodIdentifier(methodId);
        return existInDb != null ? (existInDb, true) : (methodId, false);
    }
    
    public async Task<MethodIdentifier> GetMethodIdentifier(MethodIdentifier methodId)
    {
        var inDb = await Context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        var existInDb =
            inDb.FirstOrDefault(x =>    
            x.MethodSignature == methodId.MethodSignature &&
            x.AssemblyName == methodId.AssemblyName &&
            x.ClassName == methodId.ClassName &&
            x.MethodName == methodId.MethodName);
        return existInDb;
    }


}
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

        var inDb = existInDb.Id > 0;

        return (existInDb, inDb);
    }

    public async Task<MethodIdentifier> GetMethodIdentifier(MethodIdentifier methodId)
    {
        var inDb = await _context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        if (inDb.Any() is false)
            inDb = _context
                .MethodIdentifiers
                .Local
                .Where(x => x.MethodHash == methodId.MethodHash).ToList();
        var existInDb =
            inDb.FirstOrDefault(x =>
                x.MethodSignature == methodId.MethodSignature &&
                x.AssemblyName == methodId.AssemblyName &&
                x.ClassName == methodId.ClassName &&
                x.MethodName == methodId.MethodName);

        if (existInDb is not null)
            _context.Entry(methodId).State = EntityState.Detached;
        return existInDb ?? methodId;
    }
}
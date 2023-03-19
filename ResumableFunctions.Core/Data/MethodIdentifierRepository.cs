using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core.Data;

internal class MethodIdentifierRepository : RepositoryBase
{
    public MethodIdentifierRepository(FunctionDataContext ctx) : base(ctx)
    {
    }

    public async Task<MethodIdentifier> GetMethodIdentifierFromDb(MethodData methodId)
    {
        var sameHashList = await _context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        var matchedInstance =
            sameHashList.FirstOrDefault(x =>
                x.MethodSignature == methodId.MethodSignature &&
                x.AssemblyName == methodId.AssemblyName &&
                x.ClassName == methodId.ClassName &&
                x.MethodName == methodId.MethodName);
        return matchedInstance;
    }
}
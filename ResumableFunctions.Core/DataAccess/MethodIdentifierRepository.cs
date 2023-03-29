using System.Diagnostics;
using System.Reflection;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Core.Data;

internal class MethodIdentifierRepository : RepositoryBase
{
    private ILogger<MethodIdentifierRepository> _logger;

    public MethodIdentifierRepository(FunctionDataContext ctx) : base(ctx)
    {
        _logger = CoreExtensions.GetServiceProvider().GetService<ILogger<MethodIdentifierRepository>>();
    }

    public async Task<MethodIdentifier> GetMethodIdentifierFromDb(MethodData methodId)
    {
        var sameHashList = await _context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodId.MethodHash).ToListAsync();
        return
            sameHashList.FirstOrDefault(x =>
                x.MethodSignature == methodId.MethodSignature &&
                x.AssemblyName == methodId.AssemblyName &&
                x.ClassName == methodId.ClassName &&
                x.MethodName == methodId.MethodName);
    }

    public async Task AddMethodIdentifier(MethodData methodData, MethodType methodType, string trackingId)
    {
        var methodId = await GetMethodIdentifierFromDb(methodData);
        if (methodId != null)
        {
            _logger.LogInformation($"Method [{methodData.MethodName}] already exist in DB.");
            return;
        }
        methodId =
            _context.MethodIdentifiers.Local.FirstOrDefault(x =>
                x.MethodSignature == methodData.MethodSignature &&
                x.AssemblyName == methodData.AssemblyName &&
                x.ClassName == methodData.ClassName &&
                x.MethodName == methodData.MethodName);
        if (methodId != null)
        {
            _logger.LogInformation($"Method [{methodData.MethodName}] exist in local db.");
            return;
        }

        _logger.LogInformation($"Add method [{methodData.MethodName}] to DB.");
        methodId = methodData.ToMethodIdentifier();
        methodId.Type = methodType;
        _context.MethodIdentifiers.Add(methodId);
    }
}
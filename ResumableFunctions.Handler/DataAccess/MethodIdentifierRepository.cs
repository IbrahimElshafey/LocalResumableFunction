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

    public async Task<MethodIdentifier> GetMethodIdentifierFromDb(MethodData methodData)
    {
        if (methodData.TrackingId is not null)
        {
            var result = await _context
                .MethodIdentifiers
                .FirstOrDefaultAsync(x => x.TrackingId == methodData.TrackingId);
            if (result != null)
                return result;
        }
        var sameHashList = await _context
            .MethodIdentifiers
            .Where(x => x.MethodHash == methodData.MethodHash).ToListAsync();
        return
            sameHashList.FirstOrDefault(x =>
                x.MethodSignature == methodData.MethodSignature &&
                x.AssemblyName == methodData.AssemblyName &&
                x.ClassName == methodData.ClassName &&
                x.MethodName == methodData.MethodName);
    }

    public async Task UpsertMethodIdentifier(MethodData methodData, MethodType methodType, string trackingId)
    {
        var updateMethodId = await GetMethodByTrackingId(trackingId);
        if (updateMethodId != null)
        {
            _logger.LogWarning($"Updating method ({updateMethodId}) to  ({methodData})");
            updateMethodId.MethodSignature = methodData.MethodSignature;
            updateMethodId.AssemblyName = methodData.AssemblyName;
            updateMethodId.ClassName = methodData.ClassName;
            updateMethodId.MethodName = methodData.MethodName;
            updateMethodId.TrackingId = trackingId;
            updateMethodId.MethodHash =
                MethodData.GetMethodHash(
                    methodData.MethodName,
                    methodData.ClassName,
                    methodData.AssemblyName,
                    methodData.MethodSignature);
            return;
        }

        //if add not update
        var methodId = await GetMethodIdentifierFromDb(methodData);
        if (methodId != null)
        {
            if (methodId.TrackingId != trackingId)
            {
                methodId.TrackingId = trackingId;
                _logger.LogWarning($"Tracking ID changed for method ({methodData}).");
            }
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
        methodId.TrackingId = trackingId;
        _context.MethodIdentifiers.Add(methodId);
    }

    internal async Task<MethodIdentifier> GetMethodByTrackingId(string trackingId)
    {
        if (string.IsNullOrWhiteSpace(trackingId)) return null;
        return await _context.MethodIdentifiers.FirstOrDefaultAsync(x => x.TrackingId == trackingId);
    }
}
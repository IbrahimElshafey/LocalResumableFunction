using System.Collections.Concurrent;
using System.Diagnostics;
using LocalResumableFunction.Attributes;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    /// <summary>
    ///     When method called and finished
    /// </summary>
    internal async Task MethodCalled(PushedMethod pushedMethod)
    {
        try
        {
            var methodId = await _metodIdsRepo.GetMethodIdentifierFromDb(pushedMethod.MethodData);
            if (methodId == null)
                throw new Exception(
                    $"Method [{pushedMethod.MethodData.MethodName}] is not registered in current database as [{nameof(WaitMethodAttribute)}].");
            pushedMethod.MethodId = methodId.Id;
            var matchedWaits = await _waitsRepository.GetMatchedWaits(pushedMethod);
            
            //notify sevrice that wait
            foreach (var matchedWait in matchedWaits)
            {
                matchedWait.UpdateFunctionData();
                await HandleMatchedWait(matchedWait);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.Write(ex);
            WriteMessage(ex.Message);
        }
    }

}
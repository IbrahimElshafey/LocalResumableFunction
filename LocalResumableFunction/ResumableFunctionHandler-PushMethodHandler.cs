using System.Collections.Concurrent;
using System.Diagnostics;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    /// <summary>
    ///     When method called and finished
    /// </summary>

    private static readonly ConcurrentQueue<PushedMethod> PushedMethods = new();
    private static readonly ConcurrentBag<MethodWait> PendingWaits = new();

    internal async Task MethodCalled(PushedMethod pushedMethod)
    {
        PushedMethods.Enqueue(pushedMethod);
        await Processing();
    }

    internal async Task Processing()
    {
       
        for (var i = 0; i < PushedMethods.Count; i++)
        {
            if (PushedMethods.TryDequeue(out PushedMethod current))
                await Process(current);
        }
        for (int i = 0; i < PendingWaits.Count; i++)
        {
            if (PendingWaits.TryTake(out var pendingWait))
                await ProcessWait(pendingWait);
        }

        async Task Process(PushedMethod pushedMethod)
        {
            try
            {
                var methodId = await _metodIdsRepo.GetMethodIdentifier(pushedMethod.MethodIdentifier);
                if (_context.IsInDb(methodId) is false)
                    //_context.MethodIdentifiers.Add(methodId.MethodIdentifier);
                    throw new Exception(
                        $"Method [{pushedMethod.MethodIdentifier.MethodName}] is not registered in current database as [WaitMethod].");
                pushedMethod.MethodIdentifier = methodId;
                var matchedWaits = await _waitsRepository.GetMatchedWaits(pushedMethod);
                foreach (var matchedWait in matchedWaits)
                {
                    await ProcessWait(matchedWait);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                WriteMessage(ex.Message);
            }
        }
    }

    private async Task ProcessWait(MethodWait matchedWait)
    {
        if (await CanProcessNow(matchedWait) is false) return;
        matchedWait.UpdateFunctionData();
        await HandleMatchedWait(matchedWait);
        await ProcessingFinished(matchedWait);
    }


    private async Task<bool> CanProcessNow(MethodWait matchedWait)
    {
        if (matchedWait.FunctionState.IsInProcessing)
        {
            PendingWaits.Add(matchedWait);
            return false;
        }
        matchedWait.FunctionState.IsInProcessing = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ProcessingFinished(MethodWait matchedWait)
    {
        if (PendingWaits.Contains(matchedWait))
        {
            PendingWaits.TryTake(out matchedWait);
        }
        matchedWait.FunctionState.IsInProcessing = false;
        await _context.SaveChangesAsync();
    }


   
}
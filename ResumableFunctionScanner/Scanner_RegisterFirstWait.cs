using System.Diagnostics;
using System.Reflection;
using LocalResumableFunction;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctionScanner;

internal partial class Scanner
{
    private async Task RegisterResumableFunctionFirstWait(MethodInfo resumableFunction)
    {
        WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
        var classInstance = (ResumableFunctionLocal)Activator.CreateInstance(resumableFunction.DeclaringType);
        if (classInstance != null)
        {
            try
            {
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                await functionRunner.MoveNextAsync();
                var firstWait = functionRunner.Current;
                var repo = new MethodIdentifierRepository(_context);
                var methodId = await repo.GetMethodIdentifier(resumableFunction);
                if (await FirstWaitExist(firstWait, methodId.MethodIdentifier))
                {
                    WriteMessage("First wait alerady exist.");
                    return;
                }
                firstWait.RequestedByFunction = methodId.MethodIdentifier;
                firstWait.RequestedByFunctionId = methodId.MethodIdentifier.Id;
                firstWait.IsFirst = true;
                firstWait.StateAfterWait = functionRunner.GetState();
                firstWait.FunctionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId.MethodIdentifier,
                    StateObject = classInstance,
                };
                var handler = new ResumableFunctionHandler(_context);
                await handler.GenericWaitRequested(firstWait);
                WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.Name}].");
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                WriteMessage($"Error when try to register first wait for function [{resumableFunction.Name}]");
                WriteMessage($"Error {e.Message}");
            }
        }
    }

    private Task<bool> FirstWaitExist(Wait firstWait, MethodIdentifier methodIdentifier)
    {
        return _context.Waits.AnyAsync(x =>
            x.IsFirst &&
            x.RequestedByFunctionId == methodIdentifier.Id &&
            x.Status == WaitStatus.Waiting);
    }
}
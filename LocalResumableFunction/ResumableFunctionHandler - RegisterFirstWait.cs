using System.Diagnostics;
using System.Reflection;
using LocalResumableFunction.Data;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private const string ScannerAppName = "##SCANNER: ";
    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {
        var classInstance = (ResumableFunctionLocal)Activator.CreateInstance(resumableFunction.DeclaringType);
        if (classInstance != null)
        {
            try
            {
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                if (functionRunner == null)
                {
                    WriteMessage($"Resumable function {resumableFunction.Name} not exist in code");
                    return;
                }
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
                await GenericWaitRequested(firstWait);
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
            x.Name == firstWait.Name &&
            x.Status == WaitStatus.Waiting);
    }
    private void WriteMessage(string message)
    {
        Console.Write(ScannerAppName);
        Console.WriteLine(message);
    }
}
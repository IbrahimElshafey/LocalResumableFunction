using System.Reflection;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    private const string ScannerAppName = "##SCANNER: ";

    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {
        var classInstance = (ResumableFunctionLocal)Activator.CreateInstance(resumableFunction.DeclaringType);
        if (classInstance != null)
            try
            {
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                if (functionRunner.ResumableFunctionExist is false)
                {
                    WriteMessage($"Resumable function ({resumableFunction.Name}) not exist in code.");
                    return;
                }

                await functionRunner.MoveNextAsync();
                var firstWait = functionRunner.Current;
                var methodId = await _metodIdsRepo.GetMethodIdentifier(resumableFunction);
                if (await _waitsRepository.FirstWaitExistInDb(firstWait, methodId.MethodIdentifier))
                {
                    WriteMessage("First wait already exist.");
                    return;
                }

                firstWait.RequestedByFunction = methodId.MethodIdentifier;
                firstWait.RequestedByFunctionId = methodId.MethodIdentifier.Id;
                firstWait.IsFirst = true;
                //firstWait.StateAfterWait = functionRunner.GetState();
                firstWait.FunctionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId.MethodIdentifier,
                    StateObject = classInstance
                };
                await GenericWaitRequested(firstWait);
                WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.Name}].");
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                WriteMessage($"Error when try to register first wait for function [{resumableFunction.Name}]");
                WriteMessage($"Error {e.Message}");
            }
    }

    private void WriteMessage(string message)
    {
        Console.Write(ScannerAppName);
        Console.WriteLine(message);
    }
}
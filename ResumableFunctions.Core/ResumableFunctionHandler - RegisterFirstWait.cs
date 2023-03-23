using System.Reflection;
using ResumableFunctions.Core.Data;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctions.Core;

public partial class ResumableFunctionHandler
{
    private const string ScannerAppName = "##SCANNER: ";

    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {
        //todo: change this to use bependency injection
        var classInstance = (ResumableFunctionLocal)Activator.CreateInstance(resumableFunction.DeclaringType);
        if (classInstance != null)
            try
            {
                classInstance.CurrentResumableFunction = resumableFunction;
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                if (functionRunner.ResumableFunctionExist is false)
                {
                    WriteMessage($"Resumable function ({resumableFunction.Name}) not exist in code.");
                    return;
                }

                await functionRunner.MoveNextAsync();
                var firstWait = functionRunner.Current;
                var methodId = await _metodIdsRepo.GetMethodIdentifierFromDb(new MethodData(resumableFunction));
                if (await _waitsRepository.FirstWaitExistInDb(firstWait, methodId))
                {
                    WriteMessage("First wait already exist.");
                    return;
                }

                firstWait.RequestedByFunction = methodId;
                firstWait.RequestedByFunctionId = methodId.Id;
                firstWait.IsFirst = true;
                //firstWait.StateAfterWait = functionRunner.GetState();
                firstWait.FunctionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId,
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
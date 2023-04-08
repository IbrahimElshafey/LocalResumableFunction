using System.Reflection;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler;

public partial class ResumableFunctionHandler
{
    private const string ScannerAppName = "##SCANNER: ";

    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {
        var classInstance = (ResumableFunction)ActivatorUtilities.CreateInstance(_serviceProvider, resumableFunction.DeclaringType);
        if (classInstance != null)
            try
            {
                classInstance.CurrentResumableFunction = resumableFunction;
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                if (functionRunner.ResumableFunctionExistInCode is false)
                {
                    _logger.LogWarning($"Resumable function ({resumableFunction.GetFullName()}) not exist in code.");
                    return;
                }

                await functionRunner.MoveNextAsync();
                var firstWait = functionRunner.Current;
                var methodId = await _metodIdsRepo.GetResumableFunction(new MethodData(resumableFunction));
                if (await _waitsRepository.RemoveFirstWaitIfExist(firstWait, methodId))
                {
                    //todo:expression may be changed and group may added ne one
                    WriteMessage("First wait already exist it will be deleted and recreated since it may be changed.");
                    //return;
                }

                firstWait.RequestedByFunction = methodId;
                firstWait.RequestedByFunctionId = methodId.Id;
                firstWait.IsFirst = true;
                //firstWait.StateAfterWait = functionRunner.GetState();
                var functionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId,
                    StateObject = classInstance
                };
                firstWait.FunctionState = functionState;
                functionState.LogStatus(FunctionStatus.New, $"Started and wait [{firstWait.Name}] to match.");
                await SaveWaitRequestToDb(firstWait);
                WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]");
            }
    }



    private void WriteMessage(string message)
    {
        Console.Write(ScannerAppName);
        Console.WriteLine(message);
    }

    private async Task DuplicateIfFirst(Wait currentWait)
    {
        if (currentWait.IsFirst)
            await RegisterFirstWait(currentWait.RequestedByFunction.MethodInfo);
    }

    private async Task<bool> MoveFunctionToRecycleBin(Wait lastWait)
    {
        //throw new NotImplementedException();
        return true;
    }
}
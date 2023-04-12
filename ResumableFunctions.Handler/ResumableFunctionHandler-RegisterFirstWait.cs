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
    internal async Task<Wait> CloneFirstWait(MethodWait firstWait)
    {
        MethodInfo resumableFunction = firstWait.RequestedByFunction.MethodInfo;
        var classInstance = (ResumableFunction)ActivatorUtilities.CreateInstance(_serviceProvider, resumableFunction.DeclaringType);
        if (classInstance == null)
            throw new ArgumentNullException(nameof(classInstance), $"Can't create instance of class [{resumableFunction.DeclaringType.FullName}]");
        try
        {
            classInstance.CurrentResumableFunction = resumableFunction;
            var functionRunner = new FunctionRunner(classInstance, resumableFunction);
            if (functionRunner.ResumableFunctionExistInCode is false)
            {
                string message = $"Resumable function ({resumableFunction.GetFullName()}) not exist in code.";
                _logger.LogWarning(message);
                throw new Exception(message);
            }

            await functionRunner.MoveNextAsync();
            var firstWaitClone = functionRunner.Current;

            //todo: handle cloning complex wait
            switch (firstWaitClone)
            {
                case WaitsGroup mg:break;
                case FunctionWait fw:break;
            }   
            //todo:cascade set
            firstWaitClone.RequestedByFunction = firstWait.RequestedByFunction;
            firstWaitClone.RequestedByFunctionId = firstWait.RequestedByFunction.Id;
            firstWaitClone.Status = WaitStatus.Temp;
            firstWaitClone.CascadeSetIsFirst(false);

            if (firstWait is MethodWait wait && firstWaitClone is MethodWait waitClone)
            {
                waitClone.PushedCallId = wait.PushedCallId;
                //waitClone.Input = wait.Input;
                //waitClone.Output = wait.Output;
            }

            var functionState = new ResumableFunctionState
            {
                ResumableFunctionIdentifier = firstWait.RequestedByFunction,
                StateObject = firstWait.FunctionState.StateObject
            };
            firstWaitClone.FunctionState = functionState;
            functionState.AddLog(FunctionStatus.New, $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.");
            functionState.AddLog(FunctionStatus.InProgress, $"First wait matched [{firstWaitClone.Name}] for [{resumableFunction.GetFullName()}].");
            await SaveWaitRequestToDb(firstWaitClone);
            await _context.SaveChangesAsync();
            return firstWaitClone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]");
            throw;
        }
    }
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
                firstWait.CascadeSetIsFirst(true);
                //firstWait.StateAfterWait = functionRunner.GetState();
                var functionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId,
                    StateObject = classInstance
                };
                firstWait.FunctionState = functionState;
                functionState.AddLog(FunctionStatus.New, $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.");
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
        _logger.LogInformation(message);
    }



    private async Task<bool> MoveFunctionToRecycleBin(Wait lastWait)
    {
        //move function state
        //it's logs
        //it's waits
        //to recycle bin;
        return true;
    }
}
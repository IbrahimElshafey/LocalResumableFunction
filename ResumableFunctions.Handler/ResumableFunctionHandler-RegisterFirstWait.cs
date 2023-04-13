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

    internal async Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait)
    {
        var resumableFunction = firstMatchedMethodWait.RequestedByFunction.MethodInfo;

        try
        {
            var firstWaitClone = await GetFirstWait(resumableFunction, false);
            firstWaitClone.Status = WaitStatus.Temp;
            firstWaitClone.CascadeAction(x =>
            {
                x.IsFirst = false;
                x.FunctionState.StateObject = firstMatchedMethodWait?.FunctionState?.StateObject;
            });
            firstWaitClone.FunctionState.AddLog(
                LogStatus.New, $"[{resumableFunction.GetFullName()}] started and wait [{firstMatchedMethodWait.Name}] to match.");
            firstWaitClone.FunctionState.AddLog(
                LogStatus.InProgress, $"First wait matched [{firstWaitClone.Name}] for [{resumableFunction.GetFullName()}].");
            await SaveWaitRequestToDb(firstWaitClone);//first wait clone

            var currentMw = firstWaitClone.GetChildMethodWait(firstMatchedMethodWait.Name);
            currentMw.PushedCallId = firstMatchedMethodWait.PushedCallId;
            currentMw.Status = WaitStatus.Completed;
            await _context.SaveChangesAsync();
            return currentMw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]");
            throw;
        }
    }
    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {

        try
        {
            var firstWait = await GetFirstWait(resumableFunction, true);
            if (firstWait == null)
                firstWait.FunctionState.AddLog(
                    LogStatus.New,
                    $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.");
            await SaveWaitRequestToDb(firstWait);//first wait when register function
            WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
            await _context.SaveChangesAsync();

        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]");
        }
    }

    private async Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
    {
        var classInstance = (ResumableFunction)ActivatorUtilities.CreateInstance(_serviceProvider, resumableFunction.DeclaringType);
        if (classInstance != null)
            try
            {
                classInstance.CurrentResumableFunction = resumableFunction;
                var functionRunner = new FunctionRunner(classInstance, resumableFunction);
                if (functionRunner.ResumableFunctionExistInCode is false)
                {
                    string message = $"Resumable function ({resumableFunction.GetFullName()}) not exist in code.";
                    _logger.LogWarning(message);
                    throw new NullReferenceException(message);
                }

                await functionRunner.MoveNextAsync();
                var firstWait = functionRunner.Current;
                var methodId = await _metodIdsRepo.GetResumableFunction(new MethodData(resumableFunction));
                if (removeIfExist)
                {
                    WriteMessage("First wait already exist it will be deleted and recreated since it may be changed.");
                    await _waitsRepository.RemoveFirstWaitIfExist(firstWait, methodId);
                }
                var functionState = new ResumableFunctionState
                {
                    ResumableFunctionIdentifier = methodId,
                    StateObject = classInstance
                };
                firstWait.CascadeAction(x =>
                {
                    x.RequestedByFunction = methodId;
                    x.RequestedByFunctionId = methodId.Id;
                    x.IsFirst = true;
                    x.FunctionState = functionState;
                });
                return firstWait;
            }
            catch (Exception e)
            {
                string message = $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]";
                _logger.LogError(e, message);
                throw new NullReferenceException(message);
            }
        else
        {
            throw new NullReferenceException($"Can't initiate a new instance of [{resumableFunction.DeclaringType.FullName}]");
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
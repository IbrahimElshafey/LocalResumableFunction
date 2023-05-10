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
            //firstWaitClone.FunctionState.AddLog(
            //    $"[{resumableFunction.GetFullName()}] started and wait [{firstMatchedMethodWait.Name}] to match.", LogType.Info);
          
            firstWaitClone.FunctionState.Status = FunctionStatus.InProgress;
            await SaveWaitRequestToDb(firstWaitClone);//first wait clone

            var currentMw = firstWaitClone.GetChildMethodWait(firstMatchedMethodWait.Name);
            currentMw.PushedCallId = firstMatchedMethodWait.PushedCallId;
            currentMw.Status = WaitStatus.Completed;
            currentMw.Input = firstMatchedMethodWait.Input;
            currentMw.Output = firstMatchedMethodWait.Output;
            await _context.SaveChangesAsync();
            return currentMw;
        }
        catch (Exception ex)
        {
            await LogErrorToService(resumableFunction, ex, $"Error when try to clone first wait for function [{resumableFunction.GetFullName()}]");
            throw;
        }
    }

    internal async Task RegisterFirstWait(MethodInfo resumableFunction)
    {

        try
        {
            var firstWait = await GetFirstWait(resumableFunction, true);
            if (firstWait != null)
                firstWait.FunctionState.AddLog(
                    $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.",
                    LogType.Info);
            await SaveWaitRequestToDb(firstWait);//first wait when register function
            WriteMessage($"Save first wait [{firstWait.Name}] for function [{resumableFunction.GetFullName()}].");
            await _context.SaveChangesAsync();

        }
        catch (Exception ex)
        {
            var errorMsg = $"Error when try to register first wait for function [{resumableFunction.GetFullName()}]";
            _logger.LogError(ex, errorMsg);
            await LogErrorToService(resumableFunction, ex, errorMsg);
        }
    }

    private async Task LogErrorToService(MethodInfo resumableFunction, Exception ex, string errorMsg)
    {
        _logger.LogError(ex, errorMsg);
        var assemblyName = resumableFunction.DeclaringType.Assembly.GetName().Name;
        var serviceData = await _context.ServicesData.FirstOrDefaultAsync(x => x.AssemblyName == assemblyName);
        serviceData.AddError(errorMsg, ex);
        await _context.SaveChangesAsync();
    }
    
    private async Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
    {
        var classInstance = (ResumableFunction)
            (_serviceProvider.GetService(resumableFunction.DeclaringType) ??
            ActivatorUtilities.CreateInstance(_serviceProvider, resumableFunction.DeclaringType));
        if (classInstance != null)
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
            var methodId = await _context.methodIdentifierRepo.GetResumableFunction(new MethodData(resumableFunction));
            if (removeIfExist)
            {
                WriteMessage("First wait already exist it will be deleted and recreated since it may be changed.");
                await _context.waitsRepository.RemoveFirstWaitIfExist(firstWait, methodId);
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
        else
        {
            var errorMsg = $"Can't initiate a new instance of [{resumableFunction.DeclaringType.FullName}]";
            await LogErrorToService(resumableFunction, null, errorMsg);
            throw new NullReferenceException(errorMsg);
        }
    }



    private void WriteMessage(string message)
    {
        _logger.LogInformation(message);
    }



    private async Task<bool> MoveFunctionToRecycleBin(int functionStateId)
    {
        //move function state
        //it's logs
        //it's waits
        //to recycle bin;
        return true;
    }
}
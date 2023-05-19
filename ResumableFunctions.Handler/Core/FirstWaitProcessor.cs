using System.Reflection;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.DataAccess;

namespace ResumableFunctions.Handler.Core;

internal class FirstWaitProcessor : IFirstWaitProcessor
{
    private readonly ILogger<FirstWaitProcessor> _logger;
    private readonly FunctionDataContext _context;
    private readonly ISaveWaitHandler _saveWaitHandler;
    private readonly IMethodIdentifierRepository _methodIdentifierRepo;
    private readonly IWaitsRepository _waitsRepository;
    private readonly IServiceProvider _serviceProvider;

    public FirstWaitProcessor(ILogger<FirstWaitProcessor> logger,
        ISaveWaitHandler saveWaitHandler,
        FunctionDataContext context,
        IServiceProvider serviceProvider,
        IMethodIdentifierRepository methodIdentifierRepo,
        IWaitsRepository waitsRepository)
    {
        _logger = logger;
        _context = context;
        _saveWaitHandler = saveWaitHandler;
        _serviceProvider = serviceProvider;
        _methodIdentifierRepo = methodIdentifierRepo;
        _waitsRepository = waitsRepository;
    }

    public async Task<MethodWait> CloneFirstWait(MethodWait firstMatchedMethodWait)
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
            firstWaitClone.FunctionState.Logs.AddRange(firstWaitClone.FunctionState.Logs);
            await _saveWaitHandler.SaveWaitRequestToDb(firstWaitClone);//first wait clone

            var currentMw = firstWaitClone.GetChildMethodWait(firstMatchedMethodWait.Name);
            currentMw.Status = WaitStatus.Waiting;
            currentMw.Input = firstMatchedMethodWait.Input;
            currentMw.Output = firstMatchedMethodWait.Output;

            await _context.SaveChangesAsync();
            //firstWaitClone.Status = WaitStatus.Waiting;
            return currentMw;
        }
        catch (Exception ex)
        {
            await LogErrorToService(resumableFunction, ex, $"Error when try to clone first wait for function [{resumableFunction.GetFullName()}]");
            throw;
        }
    }

    public async Task RegisterFirstWait(MethodInfo resumableFunction)
    {

        try
        {
            var firstWait = await GetFirstWait(resumableFunction, true);
            if (firstWait != null)
                firstWait.FunctionState.AddLog(
                    $"[{resumableFunction.GetFullName()}] started and wait [{firstWait.Name}] to match.",
                    LogType.Info);
            await _saveWaitHandler.SaveWaitRequestToDb(firstWait);//first wait when register function
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

    public async Task<Wait> GetFirstWait(MethodInfo resumableFunction, bool removeIfExist)
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
            var methodId = await _methodIdentifierRepo.GetResumableFunction(new MethodData(resumableFunction));
            if (removeIfExist)
            {
                WriteMessage("First wait already exist it will be deleted and recreated since it may be changed.");
                await _waitsRepository.RemoveFirstWaitIfExist(firstWait, methodId);
            }
            var service = await _context.ServicesData.FirstAsync(x => x.AssemblyName == methodId.AssemblyName);
            var functionState = new ResumableFunctionState
            {
                ResumableFunctionIdentifier = methodId,
                StateObject = classInstance,
                ServiceId = service.GetRootServiceId()
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
}
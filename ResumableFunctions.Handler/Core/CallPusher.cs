using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Reflection;

namespace ResumableFunctions.Handler.Core
{
    internal class CallPusher : ICallPusher
    {
        private readonly IUnitOfWork _context;
        private readonly IBackgroundProcess _backgroundProcess;
        private readonly IServiceQueue _serviceQueue;
        private readonly ILogger<CallPusher> _logger;
        private readonly IPushedCallsRepo _pushedCallsRepo;
        private readonly IMethodIdsRepo _methodIdsRepo;
        private readonly IServiceRepo _serviceRepo;

        public CallPusher(
            IUnitOfWork context,
            IBackgroundProcess backgroundProcess,
            IServiceQueue serviceQueue,
            ILogger<CallPusher> logger,
            IPushedCallsRepo pushedCallsRepo,
            IMethodIdsRepo methodIdsRepo,
            IServiceRepo serviceRepo)
        {
            _context = context;
            _backgroundProcess = backgroundProcess;
            _serviceQueue = serviceQueue;
            _logger = logger;
            _pushedCallsRepo = pushedCallsRepo;
            _methodIdsRepo = methodIdsRepo;
            _serviceRepo = serviceRepo;
        }

        public async Task<long> PushCall(PushedCall pushedCall)
        {
            try
            {
                await _pushedCallsRepo.Push(pushedCall);
                await _context.SaveChangesAsync();
                _backgroundProcess.Enqueue(() => _serviceQueue.RouteCallToAffectedServices(pushedCall.Id, pushedCall.MethodData.MethodUrn));
                return pushedCall.Id;
            }
            catch (Exception ex)
            {
                var error = $"Can't handle pushed call [{pushedCall}]";
                await _serviceRepo.AddErrorLog(ex, error, StatusCodes.PushedCall);
                throw new Exception(error, ex);
            }
        }
        public async Task<long> PushExternalCall(PushedCall pushedCall, string serviceName)
        {
            try
            {
                var currentServiceName = Assembly.GetEntryAssembly().GetName().Name;
                if (serviceName != currentServiceName)
                {
                    await _serviceRepo.AddErrorLog(
                        null,
                        $"Pushed call target service [{serviceName}] but the current service is [{currentServiceName}]" +
                        $"\nPushed call was [{pushedCall}]", 
                        StatusCodes.PushedCall);
                    return -1;
                }

                var methodUrn = pushedCall.MethodData.MethodUrn;
                if (await _methodIdsRepo.CanPublishFromExternal(methodUrn))
                {
                    await _pushedCallsRepo.Push(pushedCall);
                    await _context.SaveChangesAsync();
                    //Route call to current service only
                    await _serviceQueue.ProcessCallLocally(pushedCall.Id, pushedCall.MethodData.MethodUrn);
                    return pushedCall.Id;
                }
                    

                await _serviceRepo.AddLog(
                    $"There is no method with URN [{methodUrn}] that can be called from external in service [{serviceName}].\nPushed call was [{pushedCall}]",
                    LogType.Warning,
                    StatusCodes.PushedCall);
                return -1;
            }
            catch (Exception ex)
            {
                var error = $"Can't handle external pushed call [{pushedCall}]";
                await _serviceRepo.AddErrorLog(ex, error, StatusCodes.PushedCall);
                throw new Exception(error, ex);
            }
        }
    }
}
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
        private readonly IBackgroundProcess _backgroundJobClient;
        private readonly ICallProcessor _processor;
        private readonly ILogger<CallPusher> _logger;
        private readonly IPushedCallsRepo _pushedCallsRepo;
        private readonly IMethodIdsRepo _methodIdsRepo;
        private readonly IServiceRepo _serviceRepo;

        public CallPusher(
            IUnitOfWork context,
            IBackgroundProcess backgroundJobClient,
            ICallProcessor processor,
            ILogger<CallPusher> logger,
            IPushedCallsRepo pushedCallsRepo,
            IMethodIdsRepo methodIdsRepo,
            IServiceRepo serviceRepo)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _processor = processor;
            _logger = logger;
            _pushedCallsRepo = pushedCallsRepo;
            _methodIdsRepo = methodIdsRepo;
            _serviceRepo = serviceRepo;
        }

        public async Task<long> PushCall(PushedCall pushedCall)
        {
            try
            {
                _pushedCallsRepo.Push(pushedCall);
                await _context.SaveChangesAsync();
                _backgroundJobClient.Enqueue(() => _processor.InitialProcessPushedCall(pushedCall.Id, pushedCall.MethodData.MethodUrn));
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
                    return await PushCall(pushedCall);

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
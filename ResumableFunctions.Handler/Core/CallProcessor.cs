using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.ComponentModel;

namespace ResumableFunctions.Handler.Core;
internal partial class CallProcessor : ICallProcessor
{
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitsProcessor _waitsProcessor;
    private readonly IWaitsRepo _waitsRepository;
    private readonly IServiceQueue _serviceQueue;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IScanStateRepo _scanStateRepo;

    public CallProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitsProcessor waitsProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        IServiceQueue serviceQueue,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        IScanStateRepo scanStateRepo)
    {
        _logger = logger;
        _waitsProcessor = waitsProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _serviceQueue = serviceQueue;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _scanStateRepo = scanStateRepo;
    }

    [DisplayName("Initial Process Pushed Call `{0}` for MethodUrn `{1}`")]
    public async Task InitialProcessPushedCall(int pushedCallId, string methodUrn)
    {
        if (!await _scanStateRepo.IsScanFinished())
        {
            _backgroundJobClient.Schedule(() => InitialProcessPushedCall(pushedCallId, methodUrn), TimeSpan.FromSeconds(3));
            return;
        }
        
        await _backgroundJobExecutor.Execute(
            $"InitialProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var servicesImpactions = await _waitsRepository.GetAffectedServicesAndFunctions(methodUrn);
                if (servicesImpactions == null || servicesImpactions.Any() is false)
                {
                    _logger.LogWarning($"There are no services affected by pushed call `{methodUrn}:{pushedCallId}`");
                    return;
                }

                foreach (var serviceImpaction in servicesImpactions)
                {
                    serviceImpaction.PushedCallId = pushedCallId;
                    serviceImpaction.MethodUrn = methodUrn;
                    var isLocal = serviceImpaction.ServiceId == _settings.CurrentServiceId;
                    if (isLocal)
                        await ServiceProcessPushedCall(serviceImpaction);
                    else
                        await _serviceQueue.EnqueueCallImpaction(serviceImpaction);
                }
            },
            $"Error when call `InitialProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})` in service `{_settings.CurrentServiceId}`");
    }

    [DisplayName("{0}")]
    public async Task ServiceProcessPushedCall(CallServiceImapction service)
    {
        var pushedCallId = service.PushedCallId;
        await _backgroundJobExecutor.Execute(
            $"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            () =>
            {
                foreach (var functionId in service.AffectedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(
                        () => _waitsProcessor.ProcessFunctionExpectedMatchedWaits(functionId, pushedCallId, service.MethodGroupId));
                }
                return Task.CompletedTask;
            },
            $"Error when call `ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{service.MethodUrn})` in service `{_settings.CurrentServiceId}`");
    }

}
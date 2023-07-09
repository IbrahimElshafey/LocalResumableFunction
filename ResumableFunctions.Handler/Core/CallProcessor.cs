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
    private readonly HangfireHttpClient _hangFireHttpClient;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IScanStateRepo _scanStateRepo;

    public CallProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitsProcessor waitsProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        HangfireHttpClient hangFireHttpClient,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        IScanStateRepo scanStateRepo)
    {
        _logger = logger;
        _waitsProcessor = waitsProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _hangFireHttpClient = hangFireHttpClient;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _scanStateRepo = scanStateRepo;
    }

    [DisplayName("Initial Process Pushed Call `{0}` for MethodUrn `{1}`")]
    public async Task InitialProcessPushedCall(int pushedCallId, string methodUrn)
    {
        if (await _scanStateRepo.IsScanFinished())
            await _backgroundJobExecutor.Execute(
                $"InitialProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
                async () =>
                {
                    var services = await _waitsRepository.GetAffectedServices(methodUrn);
                    if (services == null || services.Any() is false)
                    {
                        _logger.LogWarning($"There is no service affected by pushed call `{methodUrn}:{pushedCallId}`");
                        return;
                    }

                    foreach (var service in services)
                    {
                        service.PushedCallId = pushedCallId;
                        service.MethodUrn = methodUrn;
                        var isLocal = service.ServiceId == _settings.CurrentServiceId;
                        if (isLocal)
                            await ServiceProcessPushedCall(service);
                        else
                            await CallOwnerService(service);
                    }
                },
                $"Error when call `InitialProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})` in service `{_settings.CurrentServiceId}`");
        else
            _backgroundJobClient.Schedule(() => InitialProcessPushedCall(pushedCallId, methodUrn), TimeSpan.FromSeconds(3));
    }

    [DisplayName("Current Service Process Pushed Call `{0}` for MethodUrn: `{1}`")]
    public async Task ServiceProcessPushedCall(AffectedService service)
    {
        var pushedCallId = service.PushedCallId;
        await _backgroundJobExecutor.Execute(
            $"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            () =>
            {
                foreach (var functionId in service.AffectedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(
                        () => _waitsProcessor.ProcessFunctionExpectedWaitMatches(functionId, pushedCallId, service.MethodGroupId));
                }
                return Task.CompletedTask;
            },
            $"Error when call `ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{service.MethodUrn})` in service `{_settings.CurrentServiceId}`");
    }

    private async Task CallOwnerService(AffectedService service)
    {
        try
        {
            var actionUrl =
                $"{service.ServiceUrl}api/ResumableFunctions/ServiceProcessPushedCall";
            await _hangFireHttpClient.EnqueueGetRequestIfFail(actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to call owner service for pushed call ({service.PushedCallId}).");
        }
    }
}
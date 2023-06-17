using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;
internal class CallProcessor : ICallProcessor
{
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitProcessor _waitProcessor;
    private readonly IWaitsRepo _waitsRepository;
    private readonly HangfireHttpClient _hangFireHttpClient;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;

    public CallProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IWaitProcessor waitProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        HangfireHttpClient hangFireHttpClient,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings)
    {
        _logger = logger;
        _waitProcessor = waitProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _hangFireHttpClient = hangFireHttpClient;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
    }

    public async Task InitialProcessPushedCall(int pushedCallId, string methodUrn)
    {
        await _backgroundJobExecutor.Execute(
            $"InitialProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var services = await _waitsRepository.GetAffectedServicesForCall(methodUrn);
                if (services == null || services.Any() is false) return;

                foreach (var service in services)
                {
                    var isLocal = service.Id == _settings.CurrentServiceId;
                    if (isLocal)
                        await ServiceProcessPushedCall(pushedCallId, methodUrn);
                    else
                        await CallOwnerService(service, pushedCallId, methodUrn);
                }
            },
            $"Error when call `InitialProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})` in service `{_settings.CurrentServiceId}`");
    }

    public async Task ServiceProcessPushedCall(int pushedCallId, string methodUrn)
    {
        await _backgroundJobExecutor.Execute(
            $"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var matchedFunctionsIds = await _waitsRepository.GetMatchedFunctionsForCall(pushedCallId, methodUrn);
                foreach (var functionId in matchedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(() => _waitProcessor.ProcessFunctionExpectedMatches(functionId, pushedCallId));
                }
            },
            $"Error when call `ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})` in service `{_settings.CurrentServiceId}`");
    }


    private async Task CallOwnerService(ServiceData service, int pushedCallId, string methodUrn)
    {
        try
        {
            var actionUrl =
                $"{service.Url}api/ResumableFunctions/ServiceProcessPushedCall?pushedCallId={pushedCallId}&methodUrn={methodUrn}";
            await _hangFireHttpClient.EnqueueGetRequestIfFail(actionUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to call owner service for pushed call ({pushedCallId}).");
        }
    }
}
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
    private readonly IExpectedMatchesProcessor _expectedMatchesProcessor;
    private readonly IWaitsRepo _waitsRepository;
    private readonly HangfireHttpClient _hangFireHttpClient;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IScanStateRepo _scanStateRepo;

    public CallProcessor(
        ILogger<ReplayWaitProcessor> logger,
        IExpectedMatchesProcessor expectedMatchesProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        HangfireHttpClient hangFireHttpClient,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        IScanStateRepo scanStateRepo)
    {
        _logger = logger;
        _expectedMatchesProcessor = expectedMatchesProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _hangFireHttpClient = hangFireHttpClient;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _scanStateRepo = scanStateRepo;
    }

    public async Task InitialProcessPushedCall(long pushedCallId, string methodUrn)
    {
        if (await _scanStateRepo.IsScanFinished())
            await _backgroundJobExecutor.Execute(
                $"InitialProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
                async () =>
                {
                    var services = await _waitsRepository.GetAffectedServicesForCall(methodUrn);
                    if (services == null || services.Any() is false)
                    {
                        return;
                    }

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
        else
            _backgroundJobClient.Schedule(() => InitialProcessPushedCall(pushedCallId, methodUrn), TimeSpan.FromSeconds(3));
    }

    public async Task ServiceProcessPushedCall(long pushedCallId, string methodUrn)
    {
        await _backgroundJobExecutor.Execute(
            $"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var matchedFunctionsIds = await _waitsRepository.GetMatchedFunctionsForCall(pushedCallId, methodUrn);
                foreach (var functionId in matchedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(() => _expectedMatchesProcessor.ProcessFunctionExpectedMatches(functionId, pushedCallId));
                }
            },
            $"Error when call `ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})` in service `{_settings.CurrentServiceId}`");
    }


    private async Task CallOwnerService(ServiceData service, long pushedCallId, string methodUrn)
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
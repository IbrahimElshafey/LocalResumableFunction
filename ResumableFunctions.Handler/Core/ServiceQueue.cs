using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.ComponentModel;
using System.Text;

namespace ResumableFunctions.Handler.Core;
internal class ServiceQueue : IServiceQueue
{
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly ILogger<ReplayWaitProcessor> _logger;
    private readonly IWaitsProcessor _waitsProcessor;
    private readonly IWaitsRepo _waitsRepository;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly IScanStateRepo _scanStateRepo;
    private readonly IHttpClientFactory _httpClientFactory;

    public ServiceQueue(
        ILogger<ReplayWaitProcessor> logger,
        IWaitsProcessor waitsProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        IScanStateRepo scanStateRepo,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _waitsProcessor = waitsProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _scanStateRepo = scanStateRepo;
        _httpClientFactory = httpClientFactory;
    }

    [DisplayName("Route call [Id: {0},MethodUrn: {1}] to services that may be affected.")]
    public async Task RouteCallToAffectedServices(long pushedCallId, string methodUrn)
    {
        //if scan is running schedule it for later processing
        if (!await _scanStateRepo.IsScanFinished())
        {
            _backgroundJobClient.Schedule(() => RouteCallToAffectedServices(pushedCallId, methodUrn), TimeSpan.FromSeconds(3));
            return;
        }

        await _backgroundJobExecutor.Execute(
            $"{nameof(RouteCallToAffectedServices)}_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var callEffections = await _waitsRepository.GetAffectedServicesAndFunctions(methodUrn);
                if (callEffections == null || callEffections.Any() is false)
                {
                    _logger.LogWarning($"There are no services affected by pushed call [{methodUrn}:{pushedCallId}]");
                    return;
                }

                foreach (var callEffection in callEffections)
                {
                    callEffection.CallId = pushedCallId;
                    callEffection.MethodUrn = methodUrn;
                    var isLocal = callEffection.AffectedServiceId == _settings.CurrentServiceId;
                    if (isLocal)
                        await ServiceProcessPushedCall(callEffection);
                    else
                        await EnqueueCallEffection(callEffection);
                }
            },
            $"Error when call [{nameof(RouteCallToAffectedServices)}(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})] in service [{_settings.CurrentServiceId}]");
    }

    [DisplayName("Process call [Id: {0},MethodUrn: {1}] Locally.")]
    public async Task ProcessCallLocally(long pushedCallId, string methodUrn)
    {
        if (!await _scanStateRepo.IsScanFinished())
        {
            _backgroundJobClient.Schedule(() => ProcessCallLocally(pushedCallId, methodUrn), TimeSpan.FromSeconds(3));
            return;
        }

        await _backgroundJobExecutor.Execute(
            $"{nameof(ProcessCallLocally)}_{pushedCallId}_{_settings.CurrentServiceId}",
            async () =>
            {
                var callEffection = await _waitsRepository.GetCallEffectionInCurrentService(methodUrn);

                if (callEffection != null)
                {
                    callEffection.CallId = pushedCallId;
                    callEffection.MethodUrn = methodUrn;
                    await ServiceProcessPushedCall(callEffection);//todo:log if null
                }
                else
                {
                    _logger.LogWarning($"There are no functions affected in current service by pushed call [{methodUrn}:{pushedCallId}]");
                }
            },
            $"Error when call [{nameof(ProcessCallLocally)}(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})] in service [{_settings.CurrentServiceId}]");
    }

    [DisplayName("{0}")]
    public async Task ServiceProcessPushedCall(CallEffection callEffection)
    {
        var pushedCallId = callEffection.CallId;
        await _backgroundJobExecutor.Execute(
            $"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
            () =>
            {
                foreach (var functionId in callEffection.AffectedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(
                        () => _waitsProcessor.ProcessFunctionExpectedWaits(functionId, pushedCallId, callEffection.MethodGroupId));
                }
                return Task.CompletedTask;
            },
            $"Error when call [ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{callEffection.MethodUrn})] in service [{_settings.CurrentServiceId}]");
    }

    [DisplayName("[{0}]")]
    public async Task EnqueueCallEffection(CallEffection callEffection)
    {
        try
        {
            var actionUrl = $"{callEffection.AffectedServiceUrl}{Constants.ResumableFunctionsControllerUrl}/{Constants.ServiceProcessPushedCallAction}";
            await DirectHttpPost(actionUrl, callEffection);// will got to ResumableFunctionsController.ServiceProcessPushedCall
        }
        catch (Exception)
        {
            _backgroundJobClient.Schedule(() => EnqueueCallEffection(callEffection), TimeSpan.FromSeconds(3));
        }
    }

    private async Task DirectHttpPost(string actionUrl, object callImapction)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonConvert.SerializeObject(callImapction), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(actionUrl, content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        if (!(result == "1" || result == "-1"))
            throw new Exception("Expected result must be 1 or -1");
    }
}
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ResumableFunctions.Handler.Core;
internal class ServiceQueue : IServiceQueue
{
    private readonly IBackgroundProcess _backgroundJobClient;
    private readonly ILogger<ServiceQueue> _logger;
    private readonly IWaitsProcessor _waitsProcessor;
    private readonly IWaitsRepo _waitsRepository;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IResumableFunctionsSettings _settings;
    private readonly ILockStateRepo _lockStateRepo;
    private readonly IHttpClientFactory _httpClientFactory;

    public ServiceQueue(
        ILogger<ServiceQueue> logger,
        IWaitsProcessor waitsProcessor,
        IWaitsRepo waitsRepository,
        IBackgroundProcess backgroundJobClient,
        BackgroundJobExecutor backgroundJobExecutor,
        IResumableFunctionsSettings settings,
        ILockStateRepo lockStateRepo,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _waitsProcessor = waitsProcessor;
        _waitsRepository = waitsRepository;
        _backgroundJobClient = backgroundJobClient;
        _backgroundJobExecutor = backgroundJobExecutor;
        _settings = settings;
        _lockStateRepo = lockStateRepo;
        _httpClientFactory = httpClientFactory;
    }

    [DisplayName("Route call [Id: {0},MethodUrn: {1}] to services that may be affected.")]
    public async Task RouteCallToAffectedServices(long pushedCallId, DateTime puhsedCallDate, string methodUrn)
    {
        //if scan is running schedule it for later processing
        if (!await _lockStateRepo.AreLocksExist())
        {
            //get current job id?
            _backgroundJobClient.Schedule(() => RouteCallToAffectedServices(pushedCallId, puhsedCallDate, methodUrn), TimeSpan.FromSeconds(3));
            return;
        }

        //$"{nameof(RouteCallToAffectedServices)}_{pushedCallId}_{_settings.CurrentServiceId}",
        //no chance to be called by two services in same time, lock removed
        await _backgroundJobExecutor.ExecuteWithoutLock(
            async () =>
            {
                var callEffections = await _waitsRepository.GetAffectedServicesAndFunctions(methodUrn, puhsedCallDate);
                if (callEffections == null || callEffections.Any() is false)
                {
                    _logger.LogWarning($"There are no services affected by pushed call [{methodUrn}:{pushedCallId}]");
                    return;
                }

                foreach (var callEffection in callEffections)
                {
                    callEffection.CallId = pushedCallId;
                    callEffection.MethodUrn = methodUrn;
                    callEffection.CallDate = puhsedCallDate;
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
    public async Task ProcessCallLocally(long pushedCallId, string methodUrn, DateTime puhsedCallDate)
    {
        if (!await _lockStateRepo.AreLocksExist())
        {
            _backgroundJobClient.Schedule(() =>
            ProcessCallLocally(pushedCallId, methodUrn, puhsedCallDate), TimeSpan.FromSeconds(3));
            return;
        }

        //$"{nameof(ProcessCallLocally)}_{pushedCallId}_{_settings.CurrentServiceId}",
        //no chance to be called by two services at same time
        await _backgroundJobExecutor.ExecuteWithoutLock(
            async () =>
            {
                var callEffection = await _waitsRepository.GetCallEffectionInCurrentService(methodUrn, puhsedCallDate);

                if (callEffection != null)
                {
                    callEffection.CallId = pushedCallId;
                    callEffection.MethodUrn = methodUrn;
                    callEffection.CallDate = puhsedCallDate;
                    await ServiceProcessPushedCall(callEffection);
                }
                else
                {
                    _logger.LogWarning($"There are no functions affected in current service by pushed call [{methodUrn}:{pushedCallId}]");
                }
            },
            $"Error when call [{nameof(ProcessCallLocally)}(pushedCallId:{pushedCallId}, methodUrn:{methodUrn})] in service [{_settings.CurrentServiceId}]");
    }

    [DisplayName("{0}")]
    public async Task ServiceProcessPushedCall(CallImpaction callEffection)
    {
        var pushedCallId = callEffection.CallId;
        //$"ServiceProcessPushedCall_{pushedCallId}_{_settings.CurrentServiceId}",
        //todo:lock if there are many service instances
        await _backgroundJobExecutor.ExecuteWithoutLock(
            () =>
            {
                foreach (var functionId in callEffection.AffectedFunctionsIds)
                {
                    _backgroundJobClient.Enqueue(
                        () => _waitsProcessor.ProcessFunctionExpectedWaits(
                            functionId, pushedCallId, callEffection.MethodGroupId, callEffection.CallDate));
                }
                return Task.CompletedTask;
            },
            $"Error when call [ServiceProcessPushedCall(pushedCallId:{pushedCallId}, methodUrn:{callEffection.MethodUrn})] in service [{_settings.CurrentServiceId}]");
    }

    [DisplayName("[{0}]")]
    public async Task EnqueueCallEffection(CallImpaction callImpaction)
    {
        try
        {
            var actionUrl = $"{callImpaction.AffectedServiceUrl}{Constants.ResumableFunctionsControllerUrl}/{Constants.ServiceProcessPushedCallAction}";
            await DirectHttpPost(actionUrl, callImpaction);// will got to ResumableFunctionsController.ServiceProcessPushedCall
        }
        catch (Exception)
        {
            _backgroundJobClient.Schedule(() => EnqueueCallEffection(callImpaction), TimeSpan.FromSeconds(3));
        }
    }

    private async Task DirectHttpPost(string actionUrl, CallImpaction callImapction)
    {
        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(callImapction);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsJsonAsync(actionUrl, content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        if (!(result == "1" || result == "-1"))
            throw new Exception("Expected result must be 1 or -1");
    }
}
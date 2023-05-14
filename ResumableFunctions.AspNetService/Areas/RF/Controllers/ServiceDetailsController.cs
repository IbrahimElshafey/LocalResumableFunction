using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class ServiceDetailsController : Controller
    {
        private readonly IUiService _uiService;
        private readonly ILogger<ServiceDetailsController> _logger;

        public ServiceDetailsController(ILogger<ServiceDetailsController> logger, IUiService uiService)
        {
            _logger = logger;
            _uiService = uiService;
        }

        public async Task<IActionResult> Index(int serviceId)
        {
            var model = new ServiceDetailsModel() { ServiceId = serviceId };
            model.SetMenu(await _uiService.GetServiceStatistics(serviceId));
            return View(model);
        }

        [ActionName(ServiceDetailsModel.PartialNames.ServiceDetails)]
        public async Task<IActionResult> ServiceInfoView(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.ServiceDetails, await _uiService.GetServiceInfo(serviceId));
        }

        [ActionName(ServiceDetailsModel.PartialNames.ServiceLogs)]
        public async Task<IActionResult> GetLogs(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.ServiceLogs, await _uiService.GetServiceLogs(serviceId));
        }

        [ActionName(ServiceDetailsModel.PartialNames.ResumabelFunctions)]
        public async Task<IActionResult> GetResumabelFunctionsAsync(int serviceId)
        {
            return PartialView(
                ServiceDetailsModel.PartialNames.ResumabelFunctions,
                await _uiService.GetFunctionsInfo(serviceId));
        }

        [ActionName(ServiceDetailsModel.PartialNames.MethodsList)]
        public async Task<IActionResult> GetMethodsListAsync(int serviceId)
        {
            return PartialView(
                ServiceDetailsModel.PartialNames.MethodsList,
                await _uiService.GetMethodsInfo(serviceId));
        }

    }
}
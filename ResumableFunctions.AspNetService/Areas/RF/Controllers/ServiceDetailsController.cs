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

        public async Task<IActionResult> Index(int id)
        {
            var model = new ServiceDetailsModel() { ServiceId = id };
            model.SetMenu();
            return View(model);
        }

        [ActionName(ServiceDetailsModel.PartialNames.ServiceDetails)]
        public IActionResult ServiceInfoView(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.ServiceDetails);
        }

        [ActionName(ServiceDetailsModel.PartialNames.ScanLogs)]
        public IActionResult GetScanLogs(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.ScanLogs);
        }

        [ActionName(ServiceDetailsModel.PartialNames.ResumabelFunctions)]
        public IActionResult GetResumabelFunctions(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.ResumabelFunctions);
        }

        [ActionName(ServiceDetailsModel.PartialNames.MethodsList)]
        public IActionResult GetMethodsList(int serviceId)
        {
            return PartialView(ServiceDetailsModel.PartialNames.MethodsList);
        }

    }
}
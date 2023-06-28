using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;

namespace ResumableFunctions.AspNetService.Areas.RF.Controllers
{
    [Area("RF")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUiService _uiService;

        public HomeController(ILogger<HomeController> logger, IUiService uiService)
        {
            _logger = logger;
            _uiService = uiService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomePageModel();
            model.SetMenu(await _uiService.GetMainStatistics());
            return View(model);
        }

        [ActionName(PartialNames.ServicesList)]
        public async Task<IActionResult> ServicesView()
        {
            return PartialView(PartialNames.ServicesList, new ServicesListModel(await _uiService.GetServicesList()));
        }


        [ActionName(PartialNames.PushedCalls)]
        public async Task<IActionResult> PushedCalls()
        {
            return PartialView(PartialNames.PushedCalls, await _uiService.GetPushedCalls(0));
        }

        [ActionName(PartialNames.LatestLogs)]
        public async Task<IActionResult> LatestLogs()
        {
            return PartialView(PartialNames.LatestLogs, await _uiService.GetLogs());
        }

        [ActionName(PartialNames.ResumableFunctions)]
        public async Task<IActionResult> GetResumableFunctionsAsync(int serviceId)
        {
            return PartialView(PartialNames.ResumableFunctions, await _uiService.GetFunctionsInfo(serviceId));
        }

        [ActionName(PartialNames.MethodsList)]
        public async Task<IActionResult> GetMethodsListAsync(int serviceId)
        {
            return PartialView(PartialNames.MethodsList, await _uiService.GetMethodsInfo(serviceId));
        }
    }
}
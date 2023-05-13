using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUiService _uiService;

        public HomeController(ILogger<HomeController> logger, IUiService uiService)
        {
            _logger = logger;
            this._uiService = uiService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomePageModel();
            model.SetMenu(await _uiService.GetMainStatistics());
            return View(model);
        }

        [ActionName(HomePageModel.PartialNames.ServicesList)]
        public async Task<IActionResult> ServicesView()
        {
            return PartialView(HomePageModel.PartialNames.ServicesList, new ServicesListModel(await _uiService.GetServicesList()));
        }

        [ActionName(HomePageModel.PartialNames.ResumableFunctions)]
        public async Task<IActionResult> ResumableFunctions()
        {
            return PartialView(HomePageModel.PartialNames.ResumableFunctions);
        }

        [ActionName(HomePageModel.PartialNames.LatestCalls)]
        public async Task<IActionResult> LatestCalls()
        {
            return PartialView(HomePageModel.PartialNames.LatestCalls);
        }

        [ActionName(HomePageModel.PartialNames.LatestLogs)]
        public async Task<IActionResult> LatestLogs()
        {
            return PartialView(HomePageModel.PartialNames.LatestLogs);
        }
    }
}
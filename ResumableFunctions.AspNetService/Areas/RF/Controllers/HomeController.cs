using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService;
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
            try
        {
            var model = new HomePageModel();
            model.SetMenu(await _uiService.GetMainStatistics());
            return View(model);
        }
            catch (Exception ex)
            {
                return View("Error",ex);
            }
        }

        [ActionName(PartialNames.ServicesList)]
        public async Task<IActionResult> ServicesView()
        {
            return PartialView(
                PartialNames.ServicesList,
                new ServicesListModel(await _uiService.GetServicesList()));
        }

        [ActionName(PartialNames.ResumableFunctions)]
        public async Task<IActionResult> ResumableFunctions()
        {
            return PartialView(PartialNames.ResumableFunctions);
        }

        [ActionName(PartialNames.PushedCalls)]
        public async Task<IActionResult> PushedCalls()
        {
            return PartialView(PartialNames.PushedCalls, await _uiService.GetPushedCalls(0));
        }

        [ActionName(PartialNames.LatestLogs)]
        public async Task<IActionResult> LatestLogs()
        {
            return PartialView(PartialNames.LatestLogs);
        }
    }
}
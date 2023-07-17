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

        public IActionResult Index()
        {
            var model = new HomePageModel();
            model.SetMenu();
            return View(model);
        }

        [ActionName(PartialNames.ServicesList)]
        public async Task<IActionResult> ServicesView()
        {
            return PartialView(PartialNames.ServicesList, new ServicesListModel(await _uiService.GetServicesSummary()));
        }


        [ActionName(PartialNames.PushedCalls)]
        public async Task<IActionResult> PushedCalls()
        {
            return PartialView(PartialNames.PushedCalls, await _uiService.GetPushedCalls(0));
        }

        [ActionName(PartialNames.LatestLogs)]
        public async Task<IActionResult> LatestLogs(int page = 0, int serviceId = -1, int statusCode = -1)
        {
            return PartialView(
                PartialNames.LatestLogs,
                new LogsViewModel
                {
                    Logs = await _uiService.GetLogs(page, serviceId, statusCode),
                    Services = await _uiService.GetServices(),
                    SelectedService = serviceId,
                    SelectedStatusCode = statusCode
                });
        }

        [ActionName(PartialNames.ResumableFunctions)]
        public async Task<IActionResult> GetResumableFunctions(int serviceId = -1, string functionName = null)
        {
            return PartialView(
                PartialNames.ResumableFunctions,
                new FunctionsViewModel
                {
                    Functions = await _uiService.GetFunctionsSummary(serviceId, functionName),
                    SelectedService = serviceId,
                    SearchTerm = functionName,
                    Services = await _uiService.GetServices(),
                });
        }

        [ActionName(PartialNames.MethodGroups)]
        public async Task<IActionResult> GetMethodGroups(int serviceId = -1, string searchTerm = null)
        {
            return PartialView(
                PartialNames.MethodGroups,
                new MethodGroupsViewModel
                {
                    MethodGroups = await _uiService.GetMethodGroupsSummary(serviceId, searchTerm),
                    SelectedService = serviceId,
                    SearchTerm = searchTerm,
                    Services = await _uiService.GetServices(),
                });
        }
    }
}
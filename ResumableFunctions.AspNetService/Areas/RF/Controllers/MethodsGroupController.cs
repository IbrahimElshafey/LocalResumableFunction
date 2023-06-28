using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;

namespace ResumableFunctions.AspNetService.Areas.RF.Controllers
{
    [Area("RF")]
    public class MethodsGroupController : Controller
    {
        private readonly ILogger<MethodsGroupController> _logger;
        private readonly IUiService _uiService;

        public MethodsGroupController(ILogger<MethodsGroupController> logger, IUiService uiService)
        {
            _logger = logger;
            _uiService = uiService;
        }

        [ActionName("MethodsInGroup")]
        public async Task<IActionResult> MethodsInGroup()
        {
            return View("MethodsInGroup");
        }

        [ActionName("MethodWaits")]
        public async Task<IActionResult> MethodWaits()
        {
            return View("MethodWaits");
        }
    }
}
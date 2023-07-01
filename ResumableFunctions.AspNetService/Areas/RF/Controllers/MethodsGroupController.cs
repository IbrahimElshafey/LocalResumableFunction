using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;
using System.Text.RegularExpressions;

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
        public async Task<IActionResult> MethodsInGroup(int groupId)
        {
            return PartialView("MethodsInGroup", await _uiService.GetMethodsInGroup(groupId));
        }

        [ActionName("MethodWaits")]
        public async Task<IActionResult> MethodWaits(int groupId)
        {
            return View("MethodWaits", await _uiService.GetWaitsForGroup(groupId));
        }
    }
}
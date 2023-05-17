using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class FunctionInstancesController : Controller
    {
        private readonly ILogger<FunctionInstancesController> _logger;
        private readonly IUiService _uiService;

        public FunctionInstancesController(ILogger<FunctionInstancesController> logger, IUiService uiService)
        {
            _logger = logger;
            this._uiService = uiService;
        }
        public async Task<IActionResult> AllInstances(int functionId, string functionName)
        {
            return View(
                new FunctionInstancesModel
                {
                    FunctionName = functionName,
                    Instances = await _uiService.GetFunctionInstances(functionId)
                });
        }
        public async Task<IActionResult> FunctionInstance()
        {
            return View();
        }

    }
}